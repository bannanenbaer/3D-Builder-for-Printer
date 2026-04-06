using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ThreeDBuilder.Services;

/// <summary>
/// Manages a long-running Python geometry server process.
/// Communicates via JSON lines on stdin/stdout.
/// </summary>
public class PythonBridge : IDisposable
{
    private Process? _process;
    private StreamWriter? _stdin;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> _pending = new();
    private CancellationTokenSource _cts = new();
    private int _requestCounter;

    public bool IsRunning => _process?.HasExited == false;

    public event Action<string>? OnError;

    /// <summary>Start the Python server process. Returns true on success.</summary>
    public async Task<bool> StartAsync()
    {
        string backendDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PythonBackend");
        string serverScript = Path.Combine(backendDir, "server.py");

        // Fallback: look next to the executable
        if (!File.Exists(serverScript))
        {
            string altDir = Path.Combine(Directory.GetCurrentDirectory(), "PythonBackend");
            serverScript = Path.Combine(altDir, "server.py");
            backendDir = altDir;
        }

        if (!File.Exists(serverScript))
        {
            OnError?.Invoke("Python server script not found: " + serverScript);
            return false;
        }

        string pythonExe = FindPython();

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = $"\"{serverScript}\"",
            WorkingDirectory = backendDir,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        try
        {
            _process = Process.Start(psi)!;
            _stdin = _process.StandardInput;
            _stdin.AutoFlush = true;

            // Start reading responses in background
            _ = ReadLoopAsync(_cts.Token);
            _ = ReadErrorLoopAsync(_cts.Token);

            // Wait for "ready" signal (up to 90s — cadquery first import can be slow)
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(90));
            var ready = await WaitForReadyAsync(timeout.Token);
            return ready;
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Failed to start Python: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> WaitForReadyAsync(CancellationToken ct)
    {
        // Read the first line from stdout which should be {"status":"ready",...}
        try
        {
            var firstLine = await _process!.StandardOutput.ReadLineAsync(ct);
            if (firstLine != null)
            {
                var obj = JObject.Parse(firstLine);
                return obj["status"]?.ToString() == "ready";
            }
        }
        catch { }
        return false;
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _process?.HasExited == false)
            {
                var line = await _process.StandardOutput.ReadLineAsync(ct);
                if (line == null) break;
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var obj = JObject.Parse(line);
                    var id = obj["id"]?.ToString();
                    if (id != null && _pending.TryRemove(id, out var tcs))
                    {
                        tcs.TrySetResult(obj);
                    }
                }
                catch { /* ignore parse errors */ }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            OnError?.Invoke($"Read loop error: {ex.Message}");
        }
    }

    private async Task ReadErrorLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _process?.HasExited == false)
            {
                var line = await _process.StandardError.ReadLineAsync(ct);
                if (line == null) break;
                // Log Python tracebacks etc. — useful for debugging
                Debug.WriteLine($"[Python stderr] {line}");
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>Send a command and await the JSON response.</summary>
    public async Task<JObject> SendAsync(string cmd, object args, int timeoutMs = 30000)
    {
        if (_stdin == null || !IsRunning)
            return ErrorResponse("Backend not running");

        string id = $"req-{Interlocked.Increment(ref _requestCounter)}";
        var payload = new Dictionary<string, object?>(
            args is Dictionary<string, object?> d ? d : ToDictionary(args))
        {
            ["id"] = id,
            ["cmd"] = cmd,
        };

        var tcs = new TaskCompletionSource<JObject>();
        _pending[id] = tcs;

        string json = JsonConvert.SerializeObject(payload);
        await _stdin.WriteLineAsync(json);

        using var timeout = new CancellationTokenSource(timeoutMs);
        timeout.Token.Register(() =>
        {
            if (_pending.TryRemove(id, out var t))
                t.TrySetResult(ErrorResponse($"Timeout after {timeoutMs}ms"));
        });

        return await tcs.Task;
    }

    // ── Convenience methods ───────────────────────────────────────────────

    public Task<JObject> PingAsync()
        => SendAsync("ping", new { });

    public Task<JObject> CreateShapeAsync(string shapeType, Dictionary<string, object> @params,
        double[] pos, double[] rot)
        => SendAsync("create_shape", new
        {
            shape_type = shapeType,
            @params,
            pos,
            rot,
        });

    public Task<JObject> ApplyFilletAsync(object sceneObject, double radius)
        => SendAsync("apply_fillet", new { @object = sceneObject, radius });

    public Task<JObject> ApplyChamferAsync(object sceneObject, double size)
        => SendAsync("apply_chamfer", new { @object = sceneObject, size });

    public Task<JObject> BooleanOpAsync(string op, object objA, object objB)
        => SendAsync("boolean_op", new { op, object_a = objA, object_b = objB });

    public Task<JObject> ImportStlAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var command = ext == ".3mf" ? "import_3mf" : "import_stl";
        return SendAsync(command, new { file_path = filePath });
    }

    public Task<JObject> CompileScadAsync(string scadCode)
        => SendAsync("compile_scad", new { scad_code = scadCode }, timeoutMs: 90000);

    public Task<JObject> ExportScadAsync(IEnumerable<object> objects)
        => SendAsync("export_scad", new { objects });

    public Task<JObject> ShapeToScadAsync(string shapeType, Dictionary<string, object> @params)
        => SendAsync("shape_to_scad", new { shape_type = shapeType, @params });

    public Task<JObject> GetShapeDefsAsync()
        => SendAsync("get_shape_defs", new { });

    public Task<JObject> CheckOpenScadAsync()
        => SendAsync("check_openscad", new { });

    public void Stop()
    {
        _cts.Cancel();
        try { _stdin?.Close(); } catch { }
        try { _process?.Kill(); } catch { }
        _process?.Dispose();
    }

    public void Dispose() => Stop();

    // ── Helpers ───────────────────────────────────────────────────────────

    private static JObject ErrorResponse(string msg)
        => JObject.FromObject(new { status = "error", message = msg });

    private static Dictionary<string, object?> ToDictionary(object obj)
    {
        var json = JsonConvert.SerializeObject(obj);
        return JsonConvert.DeserializeObject<Dictionary<string, object?>>(json) ?? new();
    }

    private static string FindPython()
    {
        // Check explicit paths first — PATH may not be updated after fresh install
        var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var explicit_paths = new[]
        {
            // System-wide install (InstallAllUsers=1 → C:\Program Files\)
            @"C:\Program Files\Python313\python.exe",
            @"C:\Program Files\Python312\python.exe",
            @"C:\Program Files\Python311\python.exe",
            @"C:\Program Files\Python310\python.exe",
            // Per-user install
            Path.Combine(localApp, @"Programs\Python\Python313\python.exe"),
            Path.Combine(localApp, @"Programs\Python\Python312\python.exe"),
            Path.Combine(localApp, @"Programs\Python\Python311\python.exe"),
            Path.Combine(localApp, @"Programs\Python\Python310\python.exe"),
        };

        foreach (var path in explicit_paths)
        {
            if (File.Exists(path))
            {
                App.WriteLog($"Python found at: {path}");
                return path;
            }
        }

        // Fallback: try by name via PATH
        string[] candidates = ["python", "python3", "py"];
        foreach (var c in candidates)
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo(c, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });
                p?.WaitForExit(2000);
                if (p?.ExitCode == 0)
                {
                    App.WriteLog($"Python found via PATH: {c}");
                    return c;
                }
            }
            catch { }
        }

        App.WriteLog("Python not found — backend will not start");
        return "python"; // Last resort
    }
}
