using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Net;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;

namespace ServiceManagementWithGUI
{
    internal class ServiceModel
    {
        static readonly string _pathToBatchFilesFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Batchfiles\");
        public static async Task InstallOrUninstallService()
        => await RunScript(ReturnServiceStatus("ReportService") == "NotInstalled" ? "InstallService.bat" : "UninstallService.bat");

        public static bool IsInternetConnectionValid()
        {
            try
            {
                using (WebClient client = new())
                using (client.OpenRead("http://google.com/generate_204"))
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsServiceSetToAutostart(string serviceName)
        {
            ManagementObjectSearcher searcher = new(
                $"SELECT * FROM Win32_Service WHERE Name = '{serviceName}'");

            foreach (ManagementObject service in searcher.Get())
                return service["StartMode"].ToString() == "Auto";

            return false;
        }

        public static string ReturnServiceStatus(string serviceName)
        {
            try 
            { 
                return new ServiceController(serviceName).Status.ToString(); 
            }
            catch (System.InvalidOperationException)
            {
                return "NotInstalled";
            }
        }

        public static async Task RunScript(string scriptName)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = Path.Combine(_pathToBatchFilesFolder, scriptName),
                    Verb = "runas"
                });
                if (!ServiceModel.IsInternetConnectionValid() && (scriptName == "StartService.bat" || scriptName == "RestartService.bat"))
                    await ServiceModel.ShowErrorMessage("Usługa zostanie uruchomiona, ale żeby maile mogły zostać wysłane, zapewnij dostęp do internetu.");
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                await ShowErrorMessage("Nie wyrażono zgody na uruchomienie jako administrator.");
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
            {
                await ShowErrorMessage("Nie znaleziono skryptu " + scriptName + ".");
            }
            catch (Exception ex)
            {
                await ShowErrorMessage("Błąd podczas uruchamiania skryptu:\n" + ex.Message);
            }
        }

        public static async Task ShowErrorMessage(string message)
        {
            await Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                MetroWindow metrowindow = Application.Current.MainWindow as MetroWindow;
                await metrowindow.ShowMessageAsync("Błąd", message);
            });
        }

        public static async Task<bool> ShowQuestionMessage(string message)
        {
            MetroDialogSettings dialogSettings = new()
            {
                AffirmativeButtonText = "Tak",
                NegativeButtonText = "Nie"
            };
            MetroWindow metrowindow = Application.Current.MainWindow as MetroWindow;
            MessageDialogResult answer = await metrowindow.ShowMessageAsync("Pytanie", message, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
            
            return answer == MessageDialogResult.Affirmative;
        }
        public static async Task StartOrStopService()
        => await RunScript(ReturnServiceStatus("ReportService") == "Running" ? "StopService.bat" : "StartService.bat");
    }
}
