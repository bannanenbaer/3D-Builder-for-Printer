using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ThreeDBuilder.Services;

namespace ThreeDBuilder.ViewModels
{
    public class UpdateViewModel : INotifyPropertyChanged
    {
        private readonly UpdateService _updateService;
        private bool _isCheckingForUpdates;
        private bool _updateAvailable;
        private string _currentVersion = "";
        private string _latestVersion = "";
        private string _releaseNotes = "";
        private string _statusMessage = "";
        private int _updateProgress;
        private bool _isDownloading;
        private UpdateService.UpdateInfo? _updateInfo;
        private bool _showUpdateDialog;

        private RelayCommand _checkForUpdatesCommand;
        private RelayCommand _downloadAndInstallCommand;
        private RelayCommand _cancelCommand;

        public event PropertyChangedEventHandler? PropertyChanged;

        public UpdateViewModel(UpdateService updateService)
        {
            _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
            InitializeCommands();
            LoadCurrentVersion();

            _updateService.UpdateProgress += (s, e) =>
            {
                StatusMessage = e.Message;
                UpdateProgress = e.ProgressPercentage;
                IsDownloading = e.Status == UpdateService.UpdateStatus.DownloadingInstaller ||
                               e.Status == UpdateService.UpdateStatus.Installing;
            };
        }

        private void InitializeCommands()
        {
            _checkForUpdatesCommand = new RelayCommand(
                _ => CheckForUpdatesAsync(),
                _ => !IsCheckingForUpdates
            );

            _downloadAndInstallCommand = new RelayCommand(
                _ => DownloadAndInstallAsync(),
                _ => UpdateAvailable && !IsDownloading
            );

            _cancelCommand = new RelayCommand(
                _ => CancelUpdate(),
                _ => IsDownloading
            );
        }

        private void LoadCurrentVersion()
        {
            CurrentVersion = _updateService.GetCurrentVersion();
        }

        private async void CheckForUpdatesAsync()
        {
            IsCheckingForUpdates = true;
            StatusMessage = "Prüfe auf Updates...";
            UpdateProgress = 0;

            try
            {
                _updateInfo = await _updateService.CheckForUpdatesAsync();

                if (_updateInfo.IsUpdateAvailable)
                {
                    UpdateAvailable = true;
                    LatestVersion = _updateInfo.LatestVersion;
                    ReleaseNotes = _updateInfo.ReleaseNotes;
                    StatusMessage = $"Update verfügbar: v{_updateInfo.LatestVersion}";
                    ShowUpdateDialog = true;
                }
                else
                {
                    UpdateAvailable = false;
                    StatusMessage = "Du verwendest bereits die neueste Version!";
                    ShowUpdateDialog = true;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Fehler: {ex.Message}";
                ShowUpdateDialog = true;
            }
            finally
            {
                IsCheckingForUpdates = false;
            }
        }

        private async void DownloadAndInstallAsync()
        {
            if (_updateInfo == null || !_updateInfo.IsUpdateAvailable)
                return;

            try
            {
                var success = await _updateService.PerformUpdateAsync(_updateInfo);
                if (success)
                {
                    StatusMessage = "Update installiert! Anwendung wird neu gestartet...";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Update fehlgeschlagen: {ex.Message}";
            }
        }

        private void CancelUpdate()
        {
            IsDownloading = false;
            StatusMessage = "Update abgebrochen";
        }

        #region Properties

        public bool IsCheckingForUpdates
        {
            get => _isCheckingForUpdates;
            set { if (_isCheckingForUpdates != value) { _isCheckingForUpdates = value; OnPropertyChanged(); } }
        }

        public bool UpdateAvailable
        {
            get => _updateAvailable;
            set { if (_updateAvailable != value) { _updateAvailable = value; OnPropertyChanged(); } }
        }

        public string CurrentVersion
        {
            get => _currentVersion;
            set { if (_currentVersion != value) { _currentVersion = value; OnPropertyChanged(); } }
        }

        public string LatestVersion
        {
            get => _latestVersion;
            set { if (_latestVersion != value) { _latestVersion = value; OnPropertyChanged(); } }
        }

        public string ReleaseNotes
        {
            get => _releaseNotes;
            set { if (_releaseNotes != value) { _releaseNotes = value; OnPropertyChanged(); } }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
        }

        public int UpdateProgress
        {
            get => _updateProgress;
            set { if (_updateProgress != value) { _updateProgress = value; OnPropertyChanged(); } }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            set { if (_isDownloading != value) { _isDownloading = value; OnPropertyChanged(); } }
        }

        public bool ShowUpdateDialog
        {
            get => _showUpdateDialog;
            set { if (_showUpdateDialog != value) { _showUpdateDialog = value; OnPropertyChanged(); } }
        }

        public ICommand CheckForUpdatesCommand => _checkForUpdatesCommand;
        public ICommand DownloadAndInstallCommand => _downloadAndInstallCommand;
        public ICommand CancelCommand => _cancelCommand;

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
