using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;
using ThreeDBuilder.Models;
using ThreeDBuilder.Services;
using ThreeDBuilder.Views;

namespace ThreeDBuilder.ViewModels
{
    public class AutoFixSceneItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public override string ToString() => Name;
    }

    public class AutoFixViewModel : INotifyPropertyChanged
    {
        private readonly AutoFixService _autoFixService;
        private readonly UndoRedoService _undoRedoService;
        private bool _canUndo;
        private bool _canRedo;
        private string _undoDescription = "Nichts zum Rückgängigmachen";
        private string _redoDescription = "Nichts zum Wiederherstellen";
        private string _historyInfo = "Keine Änderungen";
        private bool _hasAnalysisReport;
        private float _printQualityScore = 100f;
        private Brush _printQualityColor = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129));
        private ObservableCollection<string> _issuesFound = new();
        private ObservableCollection<string> _recommendations = new();
        private float _filletRadius = 1.5f;
        private float _minWallThickness = 1.2f;
        private float _minHoleSize = 2.0f;
        private bool _removeSmallHoles = true;
        private bool _fixNonManifold = true;
        private bool _smoothMesh = true;
        private PrinterProfile _selectedPrinter;
        private ObservableCollection<PrinterProfile> _availablePrinters;
        private bool _canAutoFix;
        private bool _isOptimizing;
        private string _optimizationProgress = "";
        private ObservableCollection<AutoFixSceneItem> _availableObjects = new();
        private AutoFixSceneItem? _selectedObjectForOptimization;
        private bool _hasSelectedObject;
        private string _selectedObjectInfo = "Kein Objekt ausgewählt";
        private string _analyzeButtonText = "Modell analysieren";
        private string _mascotAnimationText = "Das Maskottchen läuft über dein Objekt und optimiert es! ✨";
        private string _analysisReportTitle = "Analyse-Ergebnis:";
        private MascotAnimationView.ToolType _currentMascotTool = MascotAnimationView.ToolType.None;

        private RelayCommand _undoCommand;
        private RelayCommand _redoCommand;
        private RelayCommand _analyzeCommand;
        private RelayCommand _autoFixCommand;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AutoFixViewModel(AutoFixService autoFixService, UndoRedoService undoRedoService,
            ObservableCollection<SceneObject>? sceneObjects = null)
        {
            _autoFixService = autoFixService;
            _undoRedoService = undoRedoService;

            InitializeCommands();
            InitializePrinters();

            if (sceneObjects != null)
            {
                SyncFromScene(sceneObjects);
                sceneObjects.CollectionChanged += (_, _) => SyncFromScene(sceneObjects);
            }
            else
            {
                // Fallback: populate with placeholder until real scene is connected
                InitializeAutoFixSceneItems();
            }

            UpdateHistoryInfo();

            _undoRedoService.HistoryChanged += (s, e) => UpdateHistoryInfo();
        }

        /// <summary>Sync the available object list from the live scene collection.</summary>
        private void SyncFromScene(ObservableCollection<SceneObject> scene)
        {
            AvailableObjects.Clear();
            foreach (var obj in scene)
                AvailableObjects.Add(new AutoFixSceneItem
                {
                    Id   = obj.Id,
                    Name = obj.Name,
                    Type = obj.ShapeType,
                });
        }

        private void InitializeCommands()
        {
            _undoCommand = new RelayCommand(
                _ => _undoRedoService.Undo(),
                _ => CanUndo
            );

            _redoCommand = new RelayCommand(
                _ => _undoRedoService.Redo(),
                _ => CanRedo
            );

            _analyzeCommand = new RelayCommand(
                _ => AnalyzeModel(),
                _ => !IsOptimizing && HasSelectedObject
            );

            _autoFixCommand = new RelayCommand(
                _ => ExecuteAutoFix(),
                _ => CanAutoFix && !IsOptimizing
            );
        }

        private void InitializePrinters()
        {
            _availablePrinters = new ObservableCollection<PrinterProfile>(PrinterProfile.GetCommonPrinters());
            _selectedPrinter = PrinterProfile.Prusa;
        }

        private void InitializeAutoFixSceneItems()
        {
            // Beispiel-Objekte aus der Szene
            _availableObjects.Add(new AutoFixSceneItem { Id = "obj_1", Name = "Zylinder", Type = "Cylinder" });
            _availableObjects.Add(new AutoFixSceneItem { Id = "obj_2", Name = "Quader", Type = "Box" });
            _availableObjects.Add(new AutoFixSceneItem { Id = "obj_3", Name = "Kugel", Type = "Sphere" });
            _availableObjects.Add(new AutoFixSceneItem { Id = "obj_4", Name = "Kegel", Type = "Cone" });
            _availableObjects.Add(new AutoFixSceneItem { Id = "obj_5", Name = "Torus", Type = "Torus" });
        }

        private async void AnalyzeModel()
        {
            if (!HasSelectedObject || SelectedObjectForOptimization == null)
                return;

            IsOptimizing = true;
            OptimizationProgress = $"Analysiere '{SelectedObjectForOptimization.Name}'...";
            AnalysisReportTitle = $"Analyse-Ergebnis für '{SelectedObjectForOptimization.Name}':";
            CurrentMascotTool = MascotAnimationView.ToolType.Brush; // Pinsel für Analyse

            try
            {
                var report = await _autoFixService.AnalyzeModel(SelectedObjectForOptimization.Id);

                HasAnalysisReport = true;
                PrintQualityScore = report.EstimatedPrintSuccess;
                PrintQualityColor = GetQualityColor(report.EstimatedPrintSuccess);

                IssuesFound.Clear();
                foreach (var issue in report.Issues)
                {
                    IssuesFound.Add(issue);
                }

                Recommendations.Clear();
                foreach (var rec in _autoFixService.GetPrintingRecommendations(report))
                {
                    Recommendations.Add(rec);
                }

                CanAutoFix = report.Issues.Count > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoFixViewModel] AnalyzeModel error: {ex}");
                IssuesFound.Clear();
                IssuesFound.Add($"❌ Fehler: {ex.Message}");
            }
            finally
            {
                IsOptimizing = false;
                OptimizationProgress = "";
            }
        }

        private async void ExecuteAutoFix()
        {
            if (!HasSelectedObject || SelectedObjectForOptimization == null)
                return;

            IsOptimizing = true;
            OptimizationProgress = $"Starte AutoFix für '{SelectedObjectForOptimization.Name}'...";
            MascotAnimationText = $"Das Maskottchen läuft über '{SelectedObjectForOptimization.Name}' und optimiert es! ✨";

            try
            {
                var options = new AutoFixService.AutoFixOptions
                {
                    FilletRadius = FilletRadius,
                    MinWallThickness = MinWallThickness,
                    MinHoleSize = MinHoleSize,
                    RemoveSmallHoles = RemoveSmallHoles,
                    FixNonManifold = FixNonManifold,
                    SmoothMesh = SmoothMesh
                };

                // Create undo action
                var undoAction = new AutoFixAction(
                    SelectedObjectForOptimization.Name,
                    () => { /* Execute */ },
                    () => { /* Undo */ }
                );

                _undoRedoService.Execute(undoAction);

                OptimizationProgress = $"Optimiere '{SelectedObjectForOptimization.Name}'...";
                bool success = await _autoFixService.AutoFixModel(SelectedObjectForOptimization.Id, options);

                if (success)
                {
                    OptimizationProgress = $"✓ AutoFix für '{SelectedObjectForOptimization.Name}' abgeschlossen!";
                    // Use async delay instead of blocking Thread.Sleep
                    await System.Threading.Tasks.Task.Delay(1500);

                    // Re-analyze after fix
                    AnalyzeModel();
                }
                else
                {
                    OptimizationProgress = "❌ AutoFix fehlgeschlagen";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoFixViewModel] ExecuteAutoFix error: {ex}");
                OptimizationProgress = $"❌ Fehler: {ex.Message}";
            }
            finally
            {
                IsOptimizing = false;
            }
        }

        private void UpdateHistoryInfo()
        {
            CanUndo = _undoRedoService.CanUndo;
            CanRedo = _undoRedoService.CanRedo;
            UndoDescription = _undoRedoService.GetUndoDescription();
            RedoDescription = _undoRedoService.GetRedoDescription();
            HistoryInfo = $"Änderungen: {_undoRedoService.UndoCount} | Rückgängig: {_undoRedoService.RedoCount}";

            _undoCommand?.RaiseCanExecuteChanged();
            _redoCommand?.RaiseCanExecuteChanged();
        }

        private void OnSelectedObjectChanged()
        {
            var selected = SelectedObjectForOptimization;
            HasSelectedObject = selected != null;
            if (selected != null)
            {
                SelectedObjectInfo = $"✓ '{selected.Name}' ({selected.Type}) ausgewählt";
                AnalyzeButtonText = $"'{selected.Name}' analysieren";
                HasAnalysisReport = false;
                IssuesFound.Clear();
                Recommendations.Clear();
            }
            else
            {
                SelectedObjectInfo = "Kein Objekt ausgewählt";
                AnalyzeButtonText = "Modell analysieren";
            }

            _analyzeCommand?.RaiseCanExecuteChanged();
        }

        private Brush GetQualityColor(float score)
        {
            if (score >= 85)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Green
            else if (score >= 70)
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 158, 11)); // Orange
            else
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Red
        }

        #region Properties

        public bool CanUndo
        {
            get => _canUndo;
            set { if (_canUndo != value) { _canUndo = value; OnPropertyChanged(); } }
        }

        public bool CanRedo
        {
            get => _canRedo;
            set { if (_canRedo != value) { _canRedo = value; OnPropertyChanged(); } }
        }

        public string UndoDescription
        {
            get => _undoDescription;
            set { if (_undoDescription != value) { _undoDescription = value; OnPropertyChanged(); } }
        }

        public string RedoDescription
        {
            get => _redoDescription;
            set { if (_redoDescription != value) { _redoDescription = value; OnPropertyChanged(); } }
        }

        public string HistoryInfo
        {
            get => _historyInfo;
            set { if (_historyInfo != value) { _historyInfo = value; OnPropertyChanged(); } }
        }

        public bool HasAnalysisReport
        {
            get => _hasAnalysisReport;
            set { if (_hasAnalysisReport != value) { _hasAnalysisReport = value; OnPropertyChanged(); } }
        }

        public float PrintQualityScore
        {
            get => _printQualityScore;
            set { if (_printQualityScore != value) { _printQualityScore = value; OnPropertyChanged(); } }
        }

        public Brush PrintQualityColor
        {
            get => _printQualityColor;
            set { if (_printQualityColor != value) { _printQualityColor = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string> IssuesFound
        {
            get => _issuesFound;
            set { if (_issuesFound != value) { _issuesFound = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<string> Recommendations
        {
            get => _recommendations;
            set { if (_recommendations != value) { _recommendations = value; OnPropertyChanged(); } }
        }

        public float FilletRadius
        {
            get => _filletRadius;
            set { if (_filletRadius != value) { _filletRadius = value; OnPropertyChanged(); } }
        }

        public float MinWallThickness
        {
            get => _minWallThickness;
            set { if (_minWallThickness != value) { _minWallThickness = value; OnPropertyChanged(); } }
        }

        public float MinHoleSize
        {
            get => _minHoleSize;
            set { if (_minHoleSize != value) { _minHoleSize = value; OnPropertyChanged(); } }
        }

        public bool RemoveSmallHoles
        {
            get => _removeSmallHoles;
            set { if (_removeSmallHoles != value) { _removeSmallHoles = value; OnPropertyChanged(); } }
        }

        public bool FixNonManifold
        {
            get => _fixNonManifold;
            set { if (_fixNonManifold != value) { _fixNonManifold = value; OnPropertyChanged(); } }
        }

        public bool SmoothMesh
        {
            get => _smoothMesh;
            set { if (_smoothMesh != value) { _smoothMesh = value; OnPropertyChanged(); } }
        }

        public PrinterProfile SelectedPrinter
        {
            get => _selectedPrinter;
            set { if (_selectedPrinter != value) { _selectedPrinter = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<PrinterProfile> AvailablePrinters
        {
            get => _availablePrinters;
            set { if (_availablePrinters != value) { _availablePrinters = value; OnPropertyChanged(); } }
        }

        public bool CanAutoFix
        {
            get => _canAutoFix;
            set { if (_canAutoFix != value) { _canAutoFix = value; OnPropertyChanged(); } }
        }

        public bool IsOptimizing
        {
            get => _isOptimizing;
            set { if (_isOptimizing != value) { _isOptimizing = value; OnPropertyChanged(); } }
        }

        public string OptimizationProgress
        {
            get => _optimizationProgress;
            set { if (_optimizationProgress != value) { _optimizationProgress = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<AutoFixSceneItem> AvailableObjects
        {
            get => _availableObjects;
            set { if (_availableObjects != value) { _availableObjects = value; OnPropertyChanged(); } }
        }

        public AutoFixSceneItem? SelectedObjectForOptimization
        {
            get => _selectedObjectForOptimization;
            set
            {
                if (_selectedObjectForOptimization != value)
                {
                    _selectedObjectForOptimization = value;
                    OnPropertyChanged();
                    OnSelectedObjectChanged();
                }
            }
        }

        public bool HasSelectedObject
        {
            get => _hasSelectedObject;
            set { if (_hasSelectedObject != value) { _hasSelectedObject = value; OnPropertyChanged(); } }
        }

        public string SelectedObjectInfo
        {
            get => _selectedObjectInfo;
            set { if (_selectedObjectInfo != value) { _selectedObjectInfo = value; OnPropertyChanged(); } }
        }

        public string AnalyzeButtonText
        {
            get => _analyzeButtonText;
            set { if (_analyzeButtonText != value) { _analyzeButtonText = value; OnPropertyChanged(); } }
        }

        public string MascotAnimationText
        {
            get => _mascotAnimationText;
            set { if (_mascotAnimationText != value) { _mascotAnimationText = value; OnPropertyChanged(); } }
        }

        public string AnalysisReportTitle
        {
            get => _analysisReportTitle;
            set { if (_analysisReportTitle != value) { _analysisReportTitle = value; OnPropertyChanged(); } }
        }

        public MascotAnimationView.ToolType CurrentMascotTool
        {
            get => _currentMascotTool;
            set { if (_currentMascotTool != value) { _currentMascotTool = value; OnPropertyChanged(); } }
        }

        public ICommand UndoCommand => _undoCommand;
        public ICommand RedoCommand => _redoCommand;
        public ICommand AnalyzeCommand => _analyzeCommand;
        public ICommand AutoFixCommand => _autoFixCommand;

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
