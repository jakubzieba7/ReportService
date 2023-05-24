using Cypher;
using EmailSender;
using ReportService.Core;
using ReportService.Repositories;
using System;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Timers;

namespace ReportService
{
    public partial class ReportService : ServiceBase
    {
        //private const int SendHour = 8;
        //private const int IntervalInMinutes = 1;
        private Timer _timer;
        private ErrorRepository _errorRepository = new ErrorRepository();
        private ReportRepository _reportRepository = new ReportRepository();
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private Email _email;
        private GenerateHtmlEmail _htmlEmail = new GenerateHtmlEmail();
        private string _emailReceiver;
        private int _intervalInMinutes;
        private int _sendHour;
        private bool _isSendReport;
        private StringCipher _stringCipher =
            new StringCipher("6F468B68-F6B8-4B61-8F4E-B4E858FCB497");
        private const string NotEncryptedPasswordPrefix = "encrypt:";

        public ReportService()
        {
            InitializeComponent();

            try
            {
                _emailReceiver = ConfigurationManager.AppSettings["ReceiverEmail"];
                _intervalInMinutes = Convert.ToInt32(ConfigurationManager.AppSettings["IntervalInMinutes"]);
                _sendHour = Convert.ToInt32(ConfigurationManager.AppSettings["SendHour"]);
                _isSendReport = Convert.ToBoolean(ConfigurationManager.AppSettings["IsSendReport"]);
                _timer = new Timer(_intervalInMinutes * 60000);

                _email = new Email(new EmailParams
                {
                    HostSmtp = ConfigurationManager.AppSettings["HostSmtp"],
                    Port = Convert.ToInt32(ConfigurationManager.AppSettings["Port"]),
                    EnableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["EnableSsl"]),
                    SenderName = ConfigurationManager.AppSettings["SenderName"],
                    SenderEmail = ConfigurationManager.AppSettings["SenderEmail"],
                    SenderEmailPassword = DecryptSenderEmailPassword(),

                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private string DecryptSenderEmailPassword()
        {
            var encryptedPassword = ConfigurationManager.AppSettings["SenderEmailPassword"];

            if (encryptedPassword.StartsWith(NotEncryptedPasswordPrefix))
            {
                encryptedPassword = _stringCipher
                    .Encrypt(encryptedPassword.Replace(NotEncryptedPasswordPrefix, ""));

                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configFile.AppSettings.Settings["SenderEmailPassword"].Value = encryptedPassword;
                configFile.Save();
            }

            return _stringCipher.Decrypt(encryptedPassword);
        }

        protected override void OnStart(string[] args)
        {
            _timer.Elapsed += DoWork;
            _timer.Start();
            Logger.Info("Service started...");
        }

        private async void DoWork(object sender, ElapsedEventArgs e)
        {
            try
            {
                await SendError();
                await SendReport();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }

        }

        private async Task SendError()
        {
            var errors = _errorRepository.GetLastErrors(_intervalInMinutes);

            if (errors == null || !errors.Any())
                return;

            await _email.Send("Błędy w aplikacji", _htmlEmail.GenerateErrors(errors, _intervalInMinutes), _emailReceiver);


            Logger.Info("Error sent...");

        }

        private async Task SendReport()
        {
            if (!_isSendReport)
                return;

            var actualHour = DateTime.Now.Hour;
            if (actualHour < _sendHour)
                return;

            var report = _reportRepository.GetLastNotSentReport();

            if (report == null)
                return;

            await _email.Send("Raport dobowy", _htmlEmail.GenerateReport(report), _emailReceiver);

            _reportRepository.ReportSent(report);

            Logger.Info("Report sent...");
        }

        protected override void OnStop()
        {
            Logger.Info("Service stopped...");
        }
    }
}
