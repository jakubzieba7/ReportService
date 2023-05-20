﻿using ReportService.Models.Domains;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReportService.Repositories
{
    public class ReportRepository
    {
        public Report GetLastNotSentReport()
        {
            return new Report
            {
                Id = 1,
                Title = "R/1/2023",
                Date = new DateTime(2023, 20, 5, 17, 0, 0),
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
        }

        public void ReportSent(Report report)
        {
            report.IsSend = true;
            //zapis w bazie danych
        }

    }
}
