using System;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;
using Cipher;
using EmailSender;
using ReportService.Core;
using ReportService.Core.Domains;
using ReportService.Core.Repositories;
namespace ReportService
{
    public partial class ReportService : ServiceBase
    {
        private const string NotEncryptedPasswordPrefix = "encrypt:";
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private static ushort _intervalInMinutes;
        private static TimeSpan _sendingTime;
        private readonly Configuration configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        private readonly Email _email;
        private readonly string _emailReceiver;
        private readonly ErrorRepository _errorRepository = new ErrorRepository();
        private readonly GenerateHtmlEmail _htmlEmail = new GenerateHtmlEmail();
        private readonly ReportRepository _reportRepository = new ReportRepository();
        private readonly Timer _sendingErrorTimer;
        private readonly Timer _sendingReportTimer = new Timer(60000);
        private readonly StringCipher _stringCipher = 
            new StringCipher("E2CE151A-4512-4159-8E23-86C731B53C99");
        private bool _wasLastSendingReportFailed;
        private bool _isReportSending; //pole zapobiegające powstaniu błędu "An asynchronous call is already in progress. It must be completed or canceled before you can call this method."

        public ReportService()
        {
            InitializeComponent();
            try
            {
                _emailReceiver = configFile.AppSettings.Settings["ReceiverEmail"].Value;
                _sendingTime = TimeSpan.Parse(configFile.AppSettings.Settings["SendingTime"].Value);
                _intervalInMinutes = Convert.ToUInt16(configFile.AppSettings.Settings["ErrorCheckingIntervalInMinutes"].Value);
                _sendingErrorTimer = new Timer(_intervalInMinutes * 60000);
                _email = new Email(new EmailParams
                {
                    HostSmtp = ConfigurationManager.AppSettings["HostSmtp"],
                    Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]),
                    EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSsl"]),
                    SenderName = ConfigurationManager.AppSettings["SenderName"],
                    SenderEmail = ConfigurationManager.AppSettings["SenderEmail"],
                    SenderEmailPassword = DecryptSenderEmailPassword()
                });






                Logger.Debug("Configuration loaded:");
                Logger.Debug($"Email receiver: {_emailReceiver}");
                Logger.Debug($"Sending time: {_sendingTime}");
                Logger.Debug($"Interval: {_intervalInMinutes}");
                Logger.Debug($"Email initialized: {_email != null}");
                Logger.Debug($"Error repository initialized: {_errorRepository != null}");
                Logger.Debug($"HTML email generator initialized: {_htmlEmail != null}");








            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
            }
        }

        protected override void OnStart(string[] args)
        {
            _sendingErrorTimer.Elapsed += TrySendingError;
            _sendingErrorTimer.Start();

            _sendingReportTimer.Elapsed += TrySendingReport;
            _sendingReportTimer.Start();

            Logger.Info("Service started...");
        }

        protected override void OnStop() => Logger.Info("Service stopped...");

        private string DecryptSenderEmailPassword()
        {
            var encryptedPassword = configFile.AppSettings.Settings["SenderEmailPassword"].Value;

            if (encryptedPassword.StartsWith(NotEncryptedPasswordPrefix))
            {
                encryptedPassword = _stringCipher
                    .Encrypt(encryptedPassword.Replace(NotEncryptedPasswordPrefix, ""));

                configFile.AppSettings.Settings["SenderEmailPassword"].Value = encryptedPassword;
                configFile.Save();
            }

            return _stringCipher.Decrypt(encryptedPassword);
        }
        private async Task SendError()
        {
            Logger.Debug("Starting SendError method");
            var errors = _errorRepository.GetLastErrors();
            Logger.Debug($"GetLastErrors returned: {errors != null}");

            if (errors == null || !errors.Any())
                return;
            Logger.Debug($"Number of errors found: {errors.Count()}");
            Logger.Debug($"Email parameters - Receiver: {_emailReceiver}, Interval: {_intervalInMinutes}");

            await _email.Send("Błędy w aplikacji", _htmlEmail.GenerateErrors(errors, _intervalInMinutes), _emailReceiver);

            Logger.Info("Error sent.");
        }

        private async Task SendReport()
        {
            if (_isReportSending)
                return;

            try
            {
                TimeSpan currentTime = DateTime.Now.TimeOfDay;

                if (_wasLastSendingReportFailed)
                    goto failedSendingReportLabel;

                if (ConfigurationManager.AppSettings["EnableSendingReports"] == "false")
                    return;

                // Sprawdź czy nie minęła więcej niż minuta od zaplanowanego czasu
                if (Math.Abs((currentTime - _sendingTime).TotalMinutes) > 1)
                    return;

                failedSendingReportLabel:

                Report report = _reportRepository.GetLastNotSentReport();

                if (report == null)
                    return;

                await _email.Send("Raport dobowy", _htmlEmail.GenerateReport(report), _emailReceiver);

                _reportRepository.ReportSent(report);

                _wasLastSendingReportFailed = false;

                Logger.Info("Report sent.");
            }
            finally
            {
                _isReportSending = false;
            }
        }

        private async void TrySendingError(object sender, ElapsedEventArgs e)
        {
            try
            {
                await SendError();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
            }
        }
        private async void TrySendingReport(object sender, ElapsedEventArgs e)
        {
            try
            {
                await SendReport();
            }
            catch (Exception ex)
            {
                _wasLastSendingReportFailed = true;
                Logger.Error(ex, ex.Message);
            }
        }
    }
}
