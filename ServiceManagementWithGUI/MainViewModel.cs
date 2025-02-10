using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Diary.Commands;

namespace ServiceManagementWithGUI
{
    internal class MainViewModel :INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private static readonly Configuration _config = ConfigurationManager.OpenMappedExeConfiguration
                (
                    new ExeConfigurationFileMap
                    {
                        ExeConfigFilename = Path.GetFullPath(Path.Combine
                            (AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\ReportService\bin\Debug\ReportService.exe.config"))
                    },
                    ConfigurationUserLevel.None
                );

        readonly Dictionary<string, List<string>> _propertyErrors = [];

        bool _isAutostartEnabled;

        (
            bool enableSendingReports, 
            TimeSpan sendingTime, 
            ushort errorCheckingIntervalInMinutes, 
            string receiverEmail,
            bool wasAutostartEnabled,
            string stateOfService
        ) 
            _settingsBeforeConfirmation;

        public MainViewModel()
        {
            RestoreLastSettings();
            _settingsBeforeConfirmation.wasAutostartEnabled = ServiceModel.IsServiceSetToAutostart("ReportService");
            _settingsBeforeConfirmation.stateOfService = ServiceModel.ReturnServiceStatus("ReportService");
            _isAutostartEnabled = _settingsBeforeConfirmation.wasAutostartEnabled;
            InitializeCommands();
        }
        public ICommand AskAboutSavingAndThenStartOrStopServiceCommand { get; set; }
        public ICommand CancelSettingsCommand { get; set; }
        public ICommand CloseWindowCommand { get; set; }
        public ICommand DecreaseErrorCheckingIntervalCommand { get; set; }
        public ICommand DecrementSendingTimeCommand { get; set; }
        public ICommand IncreaseErrorCheckingIntervalCommand { get; set; }
        public ICommand IncrementSendingTimeCommand { get; set; }
        public ICommand InstallOrUninstallServiceCommand { get; set; }
        public ICommand InstallOrUninstallServiceCommandAndRefreshCommand { get; set; }
        public ICommand RefreshStateOfServiceCommand { get; set; }
        public ICommand RefreshStateOfServiceWithDelayCommand { get; set; }
        public ICommand SaveSettingsCommand { get; set; }
        public ICommand StartOrStopServiceCommand { get; set; }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        public event EventHandler RequestClose;

        public string ContentOfInstallOrUnInstallButton => StatusOfReportService == "NotInstalled" ? "Zainstaluj" : "Odinstaluj";

        public string ContentOfStartOrStopButton =>
            StatusOfReportService == "Running" ||
            StatusOfReportService == "StartPending"
            ? "Zatrzymaj" : "Uruchom";

        public bool EnableSavingSettings => !HasErrors;

        public bool EnableSendingReports
        {
            get => _settingsBeforeConfirmation.enableSendingReports;
            set
            {
                _settingsBeforeConfirmation.enableSendingReports = value;
                OnPropertyChanged();
            }
        }

        public ushort ErrorCheckingIntervalInMinutes
        {
            get => _settingsBeforeConfirmation.errorCheckingIntervalInMinutes;
            set
            {
                _settingsBeforeConfirmation.errorCheckingIntervalInMinutes = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MinutesText));
            }
        }

        public bool HasErrors => _propertyErrors.Any();

        public byte Hours
        {
            get => (byte)SendingTime.Hours;
            set
            {
                if (value > 23) value = 0;
                SendingTime = new TimeSpan(value, SendingTime.Minutes, 0);
                OnPropertyChanged();
            }
        }

        public bool IsAutostartEnabled
        {
            get => _isAutostartEnabled;
            set
            {
                _isAutostartEnabled = value;
                if (_isAutostartEnabled)
                {
                    if (ServiceModel.ReturnServiceStatus("ReportService") == "NotInstalled")
                        ServiceModel.ShowErrorMessage("Nie można dodać do autostartu niezainstalowanej usługi.");
                    else
                        ServiceModel.RunScript("SetAutostart.bat");
                }
                else
                    ServiceModel.RunScript("SetManualstart.bat");
                RefreshStateOfServiceWithDelay();
            }
        }

        public bool IsStartOrStopButtonEnabled =>
            ServiceModel.ReturnServiceStatus("ReportService") != "NotInstalled" &&
            ServiceModel.ReturnServiceStatus("ReportService") != "StartPending" &&
            ServiceModel.ReturnServiceStatus("ReportService") != "StopPending";

        public byte Minutes
        {
            get => (byte)SendingTime.Minutes;
            set
            {
                SendingTime = new TimeSpan(SendingTime.Hours, value, 0);
                OnPropertyChanged();
            }
        }

        public string MinutesText
        {
            get
            {
                if (ErrorCheckingIntervalInMinutes == 1)
                    return "minuta";

                if (ErrorCheckingIntervalInMinutes > 1 && ErrorCheckingIntervalInMinutes < 5)
                    return "minuty";

                if (ErrorCheckingIntervalInMinutes >= 5 && ErrorCheckingIntervalInMinutes <= 21)
                    return "minut";

                int lastTwoDigits = ErrorCheckingIntervalInMinutes % 100;

                if (lastTwoDigits >= 12 && lastTwoDigits <= 14)
                    return "minut";

                int lastDigit = ErrorCheckingIntervalInMinutes % 10;

                if (lastDigit >= 2 && lastDigit <= 4)
                    return "minuty";

                return "minut";
            }
        }

        public string ReceiverEmail
        {
            get => _settingsBeforeConfirmation.receiverEmail;
            set
            {
                _settingsBeforeConfirmation.receiverEmail = value;
                ValidateEmail();
                OnPropertyChanged();
                OnPropertyChanged(nameof(EnableSavingSettings));
            }
        }

        public TimeSpan SendingTime
        {
            get => _settingsBeforeConfirmation.sendingTime;
            set
            {
                _settingsBeforeConfirmation.sendingTime = value;
            }
        }

        public string StatusOfReportService => ServiceModel.ReturnServiceStatus("ReportService");

        public string StatusOfReportServiceInPolish
        => StatusOfReportService switch
        {
            "Running" => "uruchomiona",
            "Stopped" => "zatrzymana",
            "StopPending" => "zatrzymywana",
            "StartPending" => "uruchamiana",
            "NotInstalled" => "niezainstalowana",
            _ => "nieznana"
        };

        public void DecreaseErrorCheckingInterval()
        {
            if (_settingsBeforeConfirmation.errorCheckingIntervalInMinutes > 10)
                ErrorCheckingIntervalInMinutes -= 10;
        }

        public void DecrementSendingTime()
        => Hours = _settingsBeforeConfirmation.sendingTime.Hours == 0 ? (byte)23 : (byte)(_settingsBeforeConfirmation.sendingTime.Hours - 1);

        public IEnumerable GetErrors(string propertyName) =>
            _propertyErrors.ContainsKey(propertyName) ? _propertyErrors[propertyName] : null;

        public async Task HandleClosingAsync()
        {
            if (IsAnySettingChanged())
            {
                bool shouldSave = await ServiceModel.ShowQuestionMessage("Nie zapisano zmian. Czy chcesz je zapisać?");
                if (shouldSave)
                    await SaveSettings();
            }
        }

        public void IncreaseErrorCheckingInterval()
        {
            if (_settingsBeforeConfirmation.errorCheckingIntervalInMinutes < 9990)
                ErrorCheckingIntervalInMinutes += 10;
        }

        public void IncrementSendingTime()
        => Hours = _settingsBeforeConfirmation.sendingTime.Hours == 23 ? (byte)0 : (byte)(_settingsBeforeConfirmation.sendingTime.Hours + 1);

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected virtual void OnRequestClose() => RequestClose?.Invoke(this, EventArgs.Empty);

        async Task AskAboutSavingAndThenStartOrStopService()
        {
            if (IsAnySettingChanged() && ServiceModel.ReturnServiceStatus("ReportService") == "Stopped")
            {
                bool shouldSave = await ServiceModel.ShowQuestionMessage("Czy zapisać zmienione ustawienia przed uruchomieniem usługi?");
                if (shouldSave)
                    await SaveSettings();
            }
            await ServiceModel.StartOrStopService();
            await RefreshStateOfServiceWithDelay();
        }

        private void CancelSettings()
        {
            RestoreLastSettings();


            if (_settingsBeforeConfirmation.stateOfService != ServiceModel.ReturnServiceStatus("ReportService"))
            {
                if (_settingsBeforeConfirmation.stateOfService == "NotInstalled")
                    ServiceModel.InstallOrUninstallService();
                else
                    ServiceModel.StartOrStopService();
            }

            if (_settingsBeforeConfirmation.wasAutostartEnabled != _isAutostartEnabled)
                ServiceModel.RunScript(_isAutostartEnabled ? "SetManualstart.bat" : "SetAutoStart.bat");

            RefreshReportSettings();
        }

        private async Task CloseWindow()
        {
            await HandleClosingAsync();
            Application.Current.Shutdown();
        }

        void InitializeCommands()
        {
            DecrementSendingTimeCommand = new RelayCommand(o => DecrementSendingTime());
            IncrementSendingTimeCommand = new RelayCommand(o => IncrementSendingTime());
            DecreaseErrorCheckingIntervalCommand = new RelayCommand(o => DecreaseErrorCheckingInterval());
            IncreaseErrorCheckingIntervalCommand = new RelayCommand(o => IncreaseErrorCheckingInterval());
            InstallOrUninstallServiceCommand = new AsyncRelayCommand(async o => await Task.Run(() => ServiceModel.InstallOrUninstallService()));
            StartOrStopServiceCommand = new AsyncRelayCommand(async o => await AskAboutSavingAndThenStartOrStopService());
            RefreshStateOfServiceCommand = new RelayCommand(o => RefreshStateOfService());
            RefreshStateOfServiceWithDelayCommand = new AsyncRelayCommand(o => RefreshStateOfServiceWithDelay());
            InstallOrUninstallServiceCommandAndRefreshCommand = new AsyncCompositeCommand
            (
                async () => await Task.Run(() => ServiceModel.InstallOrUninstallService()),
                async () => await RefreshStateOfServiceWithDelay()
            );
            AskAboutSavingAndThenStartOrStopServiceCommand = new AsyncRelayCommand(async o => await AskAboutSavingAndThenStartOrStopService());
            CancelSettingsCommand = new RelayCommand(o => CancelSettings());
            SaveSettingsCommand = new AsyncRelayCommand(async o => await Task.Run(() => SaveSettings()));
            CloseWindowCommand = new AsyncRelayCommand(async o => await CloseWindow());
        }
        bool IsAnySettingChanged()
        {
            return _config.AppSettings.Settings["EnableSendingReports"].Value != _settingsBeforeConfirmation.enableSendingReports.ToString() ||
                   _config.AppSettings.Settings["SendingTime"].Value != _settingsBeforeConfirmation.sendingTime.ToString() ||
                   _config.AppSettings.Settings["ErrorCheckingIntervalInMinutes"].Value != _settingsBeforeConfirmation.errorCheckingIntervalInMinutes.ToString() ||
                   _config.AppSettings.Settings["ReceiverEmail"].Value != _settingsBeforeConfirmation.receiverEmail.ToString();
        }

        void RefreshReportSettings()
        {
            OnPropertyChanged(nameof(EnableSendingReports));
            OnPropertyChanged(nameof(ErrorCheckingIntervalInMinutes));
            OnPropertyChanged(nameof(ReceiverEmail));
            OnPropertyChanged(nameof(Hours));
            OnPropertyChanged(nameof(Minutes));
            OnPropertyChanged(nameof(MinutesText));
        }

        void RefreshStateOfService()
        {
            OnPropertyChanged(nameof(StatusOfReportServiceInPolish));
            OnPropertyChanged(nameof(ContentOfInstallOrUnInstallButton));
            OnPropertyChanged(nameof(ContentOfStartOrStopButton));
            _isAutostartEnabled = ServiceModel.IsServiceSetToAutostart("ReportService");
            OnPropertyChanged(nameof(IsAutostartEnabled));
            OnPropertyChanged(nameof(IsStartOrStopButtonEnabled));
        }

        async Task RefreshStateOfServiceWithDelay()
        {
            await Task.Delay(500);
            RefreshStateOfService();
        }

        private void RestoreLastSettings()
        {
            _settingsBeforeConfirmation.enableSendingReports = Convert.ToBoolean(_config.AppSettings.Settings["EnableSendingReports"].Value);
            _settingsBeforeConfirmation.sendingTime = TimeSpan.Parse(_config.AppSettings.Settings["SendingTime"].Value);
            _settingsBeforeConfirmation.errorCheckingIntervalInMinutes = Convert.ToUInt16(_config.AppSettings.Settings["ErrorCheckingIntervalInMinutes"].Value);
            _settingsBeforeConfirmation.receiverEmail = _config.AppSettings.Settings["ReceiverEmail"].Value;
        }

        private async Task SaveSettings()
        {
            try
            {
                bool isServiceRunning = false;
                bool isAnySettingChanged = IsAnySettingChanged();
                if (ServiceModel.ReturnServiceStatus("ReportService") == "Running")
                    isServiceRunning = true;

                _config.AppSettings.Settings["EnableSendingReports"].Value = _settingsBeforeConfirmation.enableSendingReports.ToString();
                _config.AppSettings.Settings["SendingTime"].Value = _settingsBeforeConfirmation.sendingTime.ToString();
                _config.AppSettings.Settings["ErrorCheckingIntervalInMinutes"].Value = _settingsBeforeConfirmation.errorCheckingIntervalInMinutes.ToString();
                if (new EmailAddressAttribute().IsValid(_settingsBeforeConfirmation.receiverEmail))
                    _config.AppSettings.Settings["ReceiverEmail"].Value = _settingsBeforeConfirmation.receiverEmail;

                ConfigurationManager.RefreshSection("appSettings"); 
                _config.Save(ConfigurationSaveMode.Modified);

                if (isServiceRunning && isAnySettingChanged)
                    await ServiceModel.RunScript("RestartService.bat");
            }
            catch (Exception exception)
            {
                await ServiceModel.ShowErrorMessage("Wystąpił błąd podczas zapisu:\n" + exception);
            }
        }        
        
        private void ValidateEmail()
        {
            List<string> errors = [];
            if (!new EmailAddressAttribute().IsValid(ReceiverEmail))
                errors.Add("Nieprawidłowy format adresu email");

            if (errors.Any())
                _propertyErrors[nameof(ReceiverEmail)] = errors;
            else
                _propertyErrors.Remove(nameof(ReceiverEmail));

            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(ReceiverEmail)));
        }
    }
}