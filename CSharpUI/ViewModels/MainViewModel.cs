using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ThreeDBuilder.Models;
using ThreeDBuilder.Services;

namespace ThreeDBuilder.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly PythonBridge? _bridge;
    public TranslationService T { get; } = App.Translations;

    // ── Scene ─────────────────────────────────────────────────────────────
    public ObservableCollection<SceneObject> SceneObjects { get; } = new();

    private SceneObject? _selectedObject;
    public SceneObject? SelectedObject
    {
        get => _selectedObject;
        set
        {
            if (_selectedObject != null) _selectedObject.IsSelected = false;
            _selectedObject = value;
            if (_selectedObject != null) _selectedObject.IsSelected = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(HasTwoSelections));
            ParameterRows.Clear();
            if (_selectedObject != null)
                BuildParameterRows(_selectedObject);
        }
    }

    public bool HasSelection => SelectedObject != null;
    public bool HasTwoSelections => SceneObjects.Count(o => o.IsSelected) == 2;

    // ── Parameter rows for properties panel ──────────────────────────────
    public ObservableCollection<ParameterRow> ParameterRows { get; } = new();

    // ── Fillet / Chamfer ──────────────────────────────────────────────────
    private double _filletRadius = 1.0;
    public double FilletRadius
    {
        get => _filletRadius;
        set { _filletRadius = value; OnPropertyChanged(); }
    }

    private double _chamferSize = 1.0;
    public double ChamferSize
    {
        get => _chamferSize;
        set { _chamferSize = value; OnPropertyChanged(); }
    }

    // ── Status ────────────────────────────────────────────────────────────
    private string _statusText = "";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; OnPropertyChanged(); }
    }

    // ── SCAD editor ───────────────────────────────────────────────────────
    private string _scadCode = "// OpenSCAD-Code hier eingeben\n// Beispiel:\ncube([20, 20, 20], center=true);";
    public string ScadCode
    {
        get => _scadCode;
        set { _scadCode = value; OnPropertyChanged(); }
    }

    private string _scadMessage = "";
    public string ScadMessage
    {
        get => _scadMessage;
        set
        {
            _scadMessage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasScadMessage));
            OnPropertyChanged(nameof(ScadMessageBrush));
        }
    }

    public bool HasScadMessage => !string.IsNullOrEmpty(_scadMessage);

    private bool _scadMessageIsError;
    public Brush ScadMessageBrush =>
        _scadMessageIsError ? Brushes.OrangeRed : Brushes.LightGreen;

    // ── Undo / Redo ───────────────────────────────────────────────────────
    private readonly Stack<List<SceneObject>> _undoStack = new();
    private readonly Stack<List<SceneObject>> _redoStack = new();

    // ── Events to signal viewport ──────────────────────────────────────────
    public event Action<SceneObject>? ObjectUpdated;
    public event Action<SceneObject>? ObjectAdded;
    public event Action<SceneObject>? ObjectRemoved;
    public event Action? SceneCleared;

    // ── Commands ──────────────────────────────────────────────────────────
    public ICommand AddShapeCommand      { get; }
    public ICommand ImportStlCommand     { get; }
    public ICommand ImportScadCommand    { get; }
    public ICommand ExportStlCommand     { get; }
    public ICommand ExportScadCommand    { get; }
    public ICommand NewSceneCommand      { get; }
    public ICommand UndoCommand          { get; }
    public ICommand RedoCommand          { get; }
    public ICommand DeleteCommand        { get; }
    public ICommand DuplicateCommand     { get; }
    public ICommand ApplyParamsCommand   { get; }
    public ICommand ApplyFilletCommand   { get; }
    public ICommand ApplyChamferCommand  { get; }
    public ICommand BoolUnionCommand     { get; }
    public ICommand BoolSubtractCommand  { get; }
    public ICommand BoolIntersectCommand { get; }
    public ICommand ScadPreviewCommand   { get; }
    public ICommand ScadExportCommand    { get; }
    public ICommand ScadFromSceneCommand { get; }

    // ── Constructor ───────────────────────────────────────────────────────
    public MainViewModel()
    {
        _bridge = App.PythonBridge;
        StatusText = T.T("status_ready");

        AddShapeCommand      = new AsyncRelayCommand(p => AddShapeAsync(p?.ToString() ?? "box"));
        ImportStlCommand     = new AsyncRelayCommand(ExecuteImportStlAsync);
        ImportScadCommand    = new RelayCommand(ExecuteImportScad);
        ExportStlCommand     = new AsyncRelayCommand(ExecuteExportStlAsync);
        ExportScadCommand    = new AsyncRelayCommand(ExecuteExportScadAsync);
        NewSceneCommand      = new RelayCommand(ExecuteNewScene);
        UndoCommand          = new AsyncRelayCommand(UndoAsync, () => CanUndo);
        RedoCommand          = new AsyncRelayCommand(RedoAsync, () => CanRedo);
        DeleteCommand        = new RelayCommand(DeleteSelected, () => HasSelection);
        DuplicateCommand     = new AsyncRelayCommand(DuplicateSelectedAsync, () => HasSelection);
        ApplyParamsCommand   = new AsyncRelayCommand(UpdateSelectedShapeAsync);
        ApplyFilletCommand   = new AsyncRelayCommand(ApplyFilletAsync, () => HasSelection);
        ApplyChamferCommand  = new AsyncRelayCommand(ApplyChamferAsync, () => HasSelection);
        BoolUnionCommand     = new AsyncRelayCommand(() => BooleanOpAsync("union"),     () => HasTwoSelections);
        BoolSubtractCommand  = new AsyncRelayCommand(() => BooleanOpAsync("cut"),       () => HasTwoSelections);
        BoolIntersectCommand = new AsyncRelayCommand(() => BooleanOpAsync("intersect"), () => HasTwoSelections);
        ScadPreviewCommand   = new AsyncRelayCommand(ExecuteScadPreviewAsync);
        ScadExportCommand    = new RelayCommand(ExecuteScadExport);
        ScadFromSceneCommand = new AsyncRelayCommand(ExecuteScadFromSceneAsync);
    }

    // ── Shape creation ────────────────────────────────────────────────────

    public async Task AddShapeAsync(string shapeType)
    {
        IsBusy = true;
        StatusText = T.T("status_loading");
        try
        {
            SaveUndoState();
            var obj = new SceneObject
            {
                Name      = $"{T.T($"shape_{shapeType}")} {SceneObjects.Count + 1}",
                ShapeType = shapeType,
                Params    = GetDefaultParams(shapeType),
            };

            if (_bridge?.IsRunning == true)
            {
                var result = await _bridge.CreateShapeAsync(
                    shapeType, obj.Params,
                    new[] { obj.PosX, obj.PosY, obj.PosZ },
                    new[] { obj.RotX, obj.RotY, obj.RotZ }
                );
                if (result["status"]?.ToString() == "ok")
                    obj.StlPath = result["stl_path"]?.ToString();
            }

            SceneObjects.Add(obj);
            SelectedObject = obj;
            ObjectAdded?.Invoke(obj);
            StatusText = T.T("status_ready");
        }
        catch (Exception ex) { StatusText = ex.Message; }
        finally { IsBusy = false; }
    }

    public async Task UpdateSelectedShapeAsync()
    {
        if (SelectedObject == null || _bridge?.IsRunning != true) return;
        IsBusy = true;
        try
        {
            foreach (var row in ParameterRows)
                SelectedObject.Params[row.Key] = row.Value;

            var result = await _bridge.CreateShapeAsync(
                SelectedObject.ShapeType, SelectedObject.Params,
                new[] { SelectedObject.PosX, SelectedObject.PosY, SelectedObject.PosZ },
                new[] { SelectedObject.RotX, SelectedObject.RotY, SelectedObject.RotZ }
            );

            if (result["status"]?.ToString() == "ok")
            {
                SelectedObject.StlPath = result["stl_path"]?.ToString();
                ObjectUpdated?.Invoke(SelectedObject);
            }
        }
        finally { IsBusy = false; }
    }

    public async Task ApplyFilletAsync()
    {
        if (SelectedObject == null) { StatusText = T.T("msg_no_selection"); return; }
        IsBusy = true;
        SaveUndoState();
        try
        {
            foreach (var row in ParameterRows)
                SelectedObject.Params[row.Key] = row.Value;

            var result = await _bridge!.ApplyFilletAsync(SelectedObject.ToBackendDict(), FilletRadius);
            if (result["status"]?.ToString() == "ok")
            {
                SelectedObject.StlPath    = result["stl_path"]?.ToString();
                SelectedObject.ShapeType  = "imported";
                ObjectUpdated?.Invoke(SelectedObject);
                StatusText = T.T("status_ready");
            }
            else
                StatusText = T.T("msg_fillet_error") + ": " + result["message"];
        }
        catch { StatusText = T.T("msg_fillet_error"); }
        finally { IsBusy = false; }
    }

    public async Task ApplyChamferAsync()
    {
        if (SelectedObject == null) { StatusText = T.T("msg_no_selection"); return; }
        IsBusy = true;
        SaveUndoState();
        try
        {
            foreach (var row in ParameterRows)
                SelectedObject.Params[row.Key] = row.Value;

            var result = await _bridge!.ApplyChamferAsync(SelectedObject.ToBackendDict(), ChamferSize);
            if (result["status"]?.ToString() == "ok")
            {
                SelectedObject.StlPath   = result["stl_path"]?.ToString();
                SelectedObject.ShapeType = "imported";
                ObjectUpdated?.Invoke(SelectedObject);
                StatusText = T.T("status_ready");
            }
            else
                StatusText = T.T("msg_chamfer_error");
        }
        catch { StatusText = T.T("msg_chamfer_error"); }
        finally { IsBusy = false; }
    }

    public async Task BooleanOpAsync(string op)
    {
        var selected = SceneObjects.Where(o => o.IsSelected).ToList();
        if (selected.Count != 2) { StatusText = T.T("msg_select_two"); return; }

        IsBusy = true;
        SaveUndoState();
        try
        {
            var result = await _bridge!.BooleanOpAsync(
                op,
                selected[0].ToBackendDict(),
                selected[1].ToBackendDict()
            );

            if (result["status"]?.ToString() == "ok")
            {
                var newObj = new SceneObject
                {
                    Name      = $"{op} ({selected[0].Name} + {selected[1].Name})",
                    ShapeType = "imported",
                    Params    = new(),
                    StlPath   = result["stl_path"]?.ToString(),
                };
                ObjectRemoved?.Invoke(selected[0]);
                ObjectRemoved?.Invoke(selected[1]);
                SceneObjects.Remove(selected[0]);
                SceneObjects.Remove(selected[1]);
                SceneObjects.Add(newObj);
                SelectedObject = newObj;
                ObjectAdded?.Invoke(newObj);
                StatusText = T.T("status_ready");
            }
        }
        finally { IsBusy = false; }
    }

    public async Task ImportStlAsync(string filePath)
    {
        IsBusy = true;
        SaveUndoState();
        try
        {
            JObject result;
            if (_bridge?.IsRunning == true)
                result = await _bridge.ImportStlAsync(filePath);
            else
                result = JObject.FromObject(new { status = "ok", stl_path = filePath });

            if (result["status"]?.ToString() == "ok")
            {
                var obj = new SceneObject
                {
                    Name      = Path.GetFileNameWithoutExtension(filePath),
                    ShapeType = "imported",
                    Params    = new(),
                    StlPath   = result["stl_path"]?.ToString(),
                };
                SceneObjects.Add(obj);
                SelectedObject = obj;
                ObjectAdded?.Invoke(obj);
                StatusText = T.T("status_ready");
            }
        }
        finally { IsBusy = false; }
    }

    public void DeleteSelected()
    {
        if (SelectedObject == null) return;
        SaveUndoState();
        ObjectRemoved?.Invoke(SelectedObject);
        SceneObjects.Remove(SelectedObject);
        SelectedObject = null;
    }

    public async Task DuplicateSelectedAsync()
    {
        if (SelectedObject == null) return;
        SaveUndoState();
        var clone = SelectedObject.Clone();
        clone.PosX += 5;
        SceneObjects.Add(clone);
        SelectedObject = clone;
        await UpdateSelectedShapeAsync();
        ObjectAdded?.Invoke(clone);
    }

    public void NewScene()
    {
        SaveUndoState();
        var toRemove = SceneObjects.ToList();
        foreach (var obj in toRemove) ObjectRemoved?.Invoke(obj);
        SceneObjects.Clear();
        SelectedObject = null;
        SceneCleared?.Invoke();
        StatusText = T.T("status_ready");
    }

    // ── File command implementations ──────────────────────────────────────

    private async Task ExecuteImportStlAsync()
    {
        var dlg = new OpenFileDialog
        {
            Title  = T.T("dlg_open_stl"),
            Filter = T.T("dlg_filter_stl"),
        };
        if (dlg.ShowDialog() == true)
            await Import3dFileAsync(dlg.FileName);
    }

    private async Task Import3dFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        // Both STL and 3MF go through the same import path
        await ImportStlAsync(filePath);
    }

    private void ExecuteImportScad()
    {
        var dlg = new OpenFileDialog
        {
            Title  = T.T("dlg_open_scad"),
            Filter = T.T("dlg_filter_scad"),
        };
        if (dlg.ShowDialog() == true)
            ScadCode = File.ReadAllText(dlg.FileName);
    }

    private async Task ExecuteExportStlAsync()
    {
        if (!SceneObjects.Any()) return;
        var dlg = new SaveFileDialog
        {
            Title      = T.T("dlg_export_stl"),
            Filter     = T.T("dlg_filter_stl_save"),
            DefaultExt = ".stl",
        };
        if (dlg.ShowDialog() != true) return;

        var obj = SelectedObject ?? SceneObjects.First();
        if (obj.StlPath != null && File.Exists(obj.StlPath))
        {
            File.Copy(obj.StlPath, dlg.FileName, overwrite: true);
            StatusText = T.T("msg_export_success");
        }
        else
            StatusText = T.T("msg_export_error");
        await Task.CompletedTask;
    }

    private async Task ExecuteExportScadAsync()
    {
        if (_bridge?.IsRunning != true) return;
        var result = await _bridge.ExportScadAsync(
            SceneObjects.Select(o => (object)o.ToBackendDict()));
        if (result["status"]?.ToString() == "ok")
        {
            var dlg = new SaveFileDialog
            {
                Title      = T.T("dlg_export_stl"),
                Filter     = T.T("dlg_filter_scad"),
                DefaultExt = ".scad",
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, result["scad_code"]?.ToString());
                StatusText = T.T("msg_export_success");
            }
        }
    }

    private void ExecuteNewScene()
    {
        if (SceneObjects.Any())
        {
            var res = MessageBox.Show(
                T.T("dlg_confirm_new"),
                T.T("dlg_confirm"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            if (res != MessageBoxResult.Yes) return;
        }
        NewScene();
    }

    private async Task ExecuteScadPreviewAsync()
    {
        if (_bridge?.IsRunning != true)
        {
            _scadMessageIsError = true;
            ScadMessage = T.T("backend_error");
            return;
        }

        IsBusy = true;
        ScadMessage = "";
        try
        {
            var result = await _bridge.CompileScadAsync(ScadCode);
            if (result["status"]?.ToString() == "ok")
            {
                string? stlPath = result["stl_path"]?.ToString();
                if (stlPath != null)
                {
                    var obj = new SceneObject
                    {
                        Name      = "OpenSCAD",
                        ShapeType = "imported",
                        Params    = new(),
                        StlPath   = stlPath,
                    };
                    SceneObjects.Add(obj);
                    SelectedObject = obj;
                    ObjectAdded?.Invoke(obj);
                    _scadMessageIsError = false;
                    ScadMessage = T.T("msg_scad_success");
                }
            }
            else
            {
                _scadMessageIsError = true;
                ScadMessage = result["message"]?.ToString() ?? T.T("msg_scad_error");
            }
        }
        catch (Exception ex)
        {
            _scadMessageIsError = true;
            ScadMessage = ex.Message;
        }
        finally { IsBusy = false; }
    }

    private void ExecuteScadExport()
    {
        var dlg = new SaveFileDialog
        {
            Title      = T.T("dlg_export_scad") is string s && s != "dlg_export_scad"
                         ? s : "SCAD exportieren",
            Filter     = T.T("dlg_filter_scad"),
            DefaultExt = ".scad",
        };
        if (dlg.ShowDialog() == true)
        {
            File.WriteAllText(dlg.FileName, ScadCode);
            StatusText = T.T("msg_export_success");
        }
    }

    private async Task ExecuteScadFromSceneAsync()
    {
        if (_bridge?.IsRunning != true) return;
        var result = await _bridge.ExportScadAsync(
            SceneObjects.Select(o => (object)o.ToBackendDict()));
        if (result["status"]?.ToString() == "ok")
            ScadCode = result["scad_code"]?.ToString() ?? "";
    }

    // ── Undo / Redo ───────────────────────────────────────────────────────

    private void SaveUndoState()
    {
        _undoStack.Push(SceneObjects.Select(o => o.Clone()).ToList());
        _redoStack.Clear();
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public async Task UndoAsync()
    {
        if (!CanUndo) return;
        _redoStack.Push(SceneObjects.Select(o => o.Clone()).ToList());
        await RestoreStateAsync(_undoStack.Pop());
    }

    public async Task RedoAsync()
    {
        if (!CanRedo) return;
        _undoStack.Push(SceneObjects.Select(o => o.Clone()).ToList());
        await RestoreStateAsync(_redoStack.Pop());
    }

    private async Task RestoreStateAsync(List<SceneObject> state)
    {
        SceneCleared?.Invoke();
        SceneObjects.Clear();
        SelectedObject = null;
        foreach (var obj in state)
        {
            SceneObjects.Add(obj);
            ObjectAdded?.Invoke(obj);
        }
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        await Task.CompletedTask;
    }

    // ── Parameter rows ─────────────────────────────────────────────────────

    private static readonly Dictionary<string, Dictionary<string, double>> _defaultParams = new()
    {
        ["box"]        = new() { ["width"] = 20, ["height"] = 20, ["depth"] = 20 },
        ["sphere"]     = new() { ["radius"] = 10 },
        ["cylinder"]   = new() { ["radius"] = 10, ["height"] = 20 },
        ["cone"]       = new() { ["radius_bottom"] = 10, ["radius_top"] = 0, ["height"] = 20 },
        ["torus"]      = new() { ["radius_major"] = 15, ["radius_minor"] = 4 },
        ["prism"]      = new() { ["sides"] = 6, ["radius"] = 10, ["height"] = 20 },
        ["pyramid"]    = new() { ["base_size"] = 20, ["height"] = 20 },
        ["tube"]       = new() { ["radius_outer"] = 10, ["radius_inner"] = 7, ["height"] = 20 },
        ["ellipsoid"]  = new() { ["rx"] = 15, ["ry"] = 10, ["rz"] = 8 },
        ["hemisphere"] = new() { ["radius"] = 10 },
        ["l_profile"]  = new() { ["width"] = 20, ["height"] = 20, ["thickness"] = 3, ["length"] = 30 },
        ["t_profile"]  = new() { ["width"] = 20, ["height"] = 20, ["thickness"] = 3, ["length"] = 30 },
        ["star"]       = new() { ["outer_r"] = 15, ["inner_r"] = 7, ["points"] = 5, ["height"] = 5 },
        ["polygon"]    = new() { ["sides"] = 6, ["radius"] = 10, ["height"] = 20 },
        ["thread_cyl"] = new() { ["radius"] = 10, ["height"] = 20, ["pitch"] = 2 },
        ["imported"]   = new(),
    };

    private Dictionary<string, object> GetDefaultParams(string shapeType)
    {
        if (_defaultParams.TryGetValue(shapeType, out var d))
            return d.ToDictionary(kv => kv.Key, kv => (object)kv.Value);
        return new();
    }

    private void BuildParameterRows(SceneObject obj)
    {
        if (!_defaultParams.TryGetValue(obj.ShapeType, out var defaults)) return;
        foreach (var kv in defaults)
        {
            double val = obj.Params.TryGetValue(kv.Key, out var v)
                ? Convert.ToDouble(v) : kv.Value;
            ParameterRows.Add(new ParameterRow
            {
                Key       = kv.Key,
                Label     = T.T($"param_{kv.Key}"),
                Value     = val,
                IsInteger = kv.Key is "sides" or "points",
            });
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Represents one editable parameter in the properties panel.</summary>
public class ParameterRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Key       { get; init; } = "";
    public string Label     { get; init; } = "";
    public bool   IsInteger { get; init; }

    private double _value;
    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }
    }
}
