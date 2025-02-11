using System;
using System.Collections.Generic;
using EmailSender;
using ReportService.Core;
using ReportService.Core.Domains;

namespace ReportService.ConsoleApp
{
    class Program
    {
        static void Main()
        {

            var emailReceiver = "marko1024@seznam.cz";

            var htmlEmail = new GenerateHtmlEmail();

            var email = new Email(new EmailParams
            {
                HostSmtp = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                SenderName = "Marek Wypiór",
                SenderEmail = "testingxenial@gmail.com",
                SenderEmailPassword = ""
            });

            var report = new Report
            {
                Id = 1,
                Title = $"R/{DateTime.Now.Month}/{DateTime.Now.Year}",
                Date = DateTime.Now,
                Positions = new List<ReportPosition>
                {
                    new ReportPosition
                    {
                        Id = 1,
                        ReportId = 1,
                        Title = "Position 1",
                        Description = "Description 1",
                        Value = 43.01m
                    },
                    new ReportPosition
                    {
                        Id = 2,
                        ReportId = 1,
                        Title = "Position 2",
                        Description = "Description 2",
                        Value = 4311m
                    },
                    new ReportPosition
                    {
                        Id = 3,
                        ReportId = 1,
                        Title = "Position 3",
                        Description = "Description 3",
                        Value = 1.99m
                    }
                }
            };

            var errors = new List<Error>
            {
                new Error { Message = "Błąd testowy 1", Date = DateTime.Now },
                new Error { Message = "Błąd testowy 2", Date = DateTime.Now }
            };

            Console.WriteLine("Wysyłanie e-mail (Raport dobowy)...");

            email.Send("Raport dobowy", htmlEmail.GenerateReport(report), emailReceiver).Wait();

            Console.WriteLine("Wysłano e-mail (Raport dobowy)...");

            Console.WriteLine("Wysyłanie e-mail (Błędy w aplikacji)...");

            email.Send("Błędy w aplikacji", htmlEmail.GenerateErrors(errors, 10), emailReceiver).Wait();

            Console.WriteLine("Wysłano e-mail (Błędy w aplikacji)...");

        }
    }
}
