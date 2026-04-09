using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThreeDBuilder.Services
{
    /// <summary>
    /// Service für Versionsprüfung und Anwendungs-Updates
    /// </summary>
    public class UpdateService : IDisposable
    {
        private const string GitHubApiUrl = "https://api.github.com/repos/bannanenbaer/3D-Builder-for-Printer/releases/latest";
        private const string CurrentVersion = "1.0.0";
        private readonly HttpClient _httpClient;

        public event EventHandler<UpdateCheckEventArgs>? UpdateCheckCompleted;
        public event EventHandler<UpdateProgressEventArgs>? UpdateProgress;

        public class UpdateInfo
        {
            public string LatestVersion { get; set; } = "";
            public string? DownloadUrl { get; set; }
            public string ReleaseNotes { get; set; } = "";
            public DateTime PublishedAt { get; set; }
            public bool IsUpdateAvailable { get; set; }
        }

        public class UpdateCheckEventArgs : EventArgs
        {
            public UpdateInfo? UpdateInfo { get; set; }
            public Exception? Error { get; set; }
        }

        public class UpdateProgressEventArgs : EventArgs
        {
            public string Message { get; set; } = "";
            public int ProgressPercentage { get; set; }
            public UpdateStatus Status { get; set; }
        }

        public enum UpdateStatus
        {
            Checking,
            DownloadingInstaller,
            InstallerReady,
            Installing,
            Complete,
            Error,
            Cancelled
        }

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "3D-Builder-Pro");
        }

        /// <summary>
        /// Prüft auf verfügbare Updates
        /// </summary>
        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                RaiseUpdateProgress("Prüfe auf Updates...", 0, UpdateStatus.Checking);

                var response = await _httpClient.GetAsync(GitHubApiUrl);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                var tagName = root.GetProperty("tag_name").GetString();
                var latestVersion = tagName?.TrimStart('v') ?? "0.0.0";
                var releaseNotes = root.GetProperty("body").GetString() ?? "";
                var publishedAt = DateTime.Parse(root.GetProperty("published_at").GetString() ?? DateTime.Now.ToString());

                // Finde den Setup.exe Installer
                string? downloadUrl = null;
                var assets = root.GetProperty("assets");
                foreach (var asset in assets.EnumerateArray())
                {
                    var name = asset.GetProperty("name").GetString();
                    if (name?.EndsWith("-Setup.exe") == true || name?.EndsWith("Setup.exe") == true)
                    {
                        downloadUrl = asset.GetProperty("browser_download_url").GetString();
                        break;
                    }
                }

                var isUpdateAvailable = CompareVersions(latestVersion, CurrentVersion) > 0;

                var updateInfo = new UpdateInfo
                {
                    LatestVersion = latestVersion,
                    DownloadUrl = downloadUrl,
                    ReleaseNotes = releaseNotes,
                    PublishedAt = publishedAt,
                    IsUpdateAvailable = isUpdateAvailable
                };

                RaiseUpdateProgress(
                    isUpdateAvailable ? $"Update verfügbar: v{latestVersion}" : "Du verwendest die neueste Version",
                    100,
                    UpdateStatus.Complete
                );

                UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs { UpdateInfo = updateInfo });
                return updateInfo;
            }
            catch (Exception ex)
            {
                RaiseUpdateProgress($"Fehler bei Update-Prüfung: {ex.Message}", 0, UpdateStatus.Error);
                var errorInfo = new UpdateInfo { IsUpdateAvailable = false };
                UpdateCheckCompleted?.Invoke(this, new UpdateCheckEventArgs { UpdateInfo = errorInfo, Error = ex });
                return errorInfo;
            }
        }

        /// <summary>
        /// Lädt den Installer herunter
        /// </summary>
        public async Task<string> DownloadInstallerAsync(string downloadUrl)
        {
            try
            {
                RaiseUpdateProgress("Lade Installer herunter...", 0, UpdateStatus.DownloadingInstaller);

                var tempPath = Path.Combine(Path.GetTempPath(), "3DBuilderPro-Setup.exe");

                using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                    var canReportProgress = totalBytes != -1;

                    using (var contentStream = await response.Content.ReadAsStreamAsync())
                    using (var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                    {
                        var totalRead = 0L;
                        var buffer = new byte[8192];
                        int read;

                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;

                            if (canReportProgress)
                            {
                                var percentage = (int)((totalRead * 100) / totalBytes);
                                RaiseUpdateProgress($"Lade herunter: {percentage}%", percentage, UpdateStatus.DownloadingInstaller);
                            }
                        }
                    }
                }

                RaiseUpdateProgress("Installer bereit", 100, UpdateStatus.InstallerReady);
                return tempPath;
            }
            catch (Exception ex)
            {
                RaiseUpdateProgress($"Download-Fehler: {ex.Message}", 0, UpdateStatus.Error);
                throw;
            }
        }

        /// <summary>
        /// Startet die Installer-Installation
        /// </summary>
        public Task InstallUpdateAsync(string installerPath)
        {
            try
            {
                RaiseUpdateProgress("Starte Installer...", 0, UpdateStatus.Installing);

                var processInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    UseShellExecute = true,
                    Verb = "runas" // Führe mit Admin-Rechten aus
                };

                var proc = Process.Start(processInfo);
                if (proc == null)
                    throw new InvalidOperationException("Installer-Prozess konnte nicht gestartet werden.");

                System.Windows.Application.Current.Dispatcher.Invoke(
                    () => System.Windows.Application.Current.Shutdown());
            }
            catch (Exception ex)
            {
                RaiseUpdateProgress($"Installations-Fehler: {ex.Message}", 0, UpdateStatus.Error);
                throw;
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Führt kompletten Update-Prozess durch
        /// </summary>
        public async Task<bool> PerformUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                if (!updateInfo.IsUpdateAvailable || string.IsNullOrEmpty(updateInfo.DownloadUrl))
                {
                    return false;
                }

                var installerPath = await DownloadInstallerAsync(updateInfo.DownloadUrl);
                await InstallUpdateAsync(installerPath);

                return true;
            }
            catch (Exception ex)
            {
                RaiseUpdateProgress($"Update fehlgeschlagen: {ex.Message}", 0, UpdateStatus.Error);
                return false;
            }
        }

        /// <summary>
        /// Vergleicht zwei Versionsnummern. Gibt 0 zurück wenn Parsing fehlschlägt.
        /// </summary>
        private static int CompareVersions(string version1, string version2)
        {
            if (!Version.TryParse(version1, out var v1)) return 0;
            if (!Version.TryParse(version2, out var v2)) return 0;
            return v1.CompareTo(v2);
        }

        /// <summary>
        /// Gibt die aktuelle Version zurück
        /// </summary>
        public string GetCurrentVersion()
        {
            return CurrentVersion;
        }

        private void RaiseUpdateProgress(string message, int percentage, UpdateStatus status)
        {
            UpdateProgress?.Invoke(this, new UpdateProgressEventArgs
            {
                Message = message,
                ProgressPercentage = percentage,
                Status = status
            });
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }
}
