using System;
using System.Collections.Generic;
using ReportService.Core.Domains;

namespace ReportService.Core.Repositories
{
    public class ErrorRepository
    {
        public List<Error> GetLastErrors()
        {
            //pobieranie z bazy danych

            return new List<Error>
            {
                new Error { Message = "Błąd testowy 1", Date = DateTime.Now },
                new Error { Message = "Błąd testowy 2", Date = DateTime.Now }
            };
        }
    }
}
