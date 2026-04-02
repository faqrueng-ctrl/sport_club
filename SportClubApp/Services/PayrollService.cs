using System.Collections.Generic;
using SportClubApp.Data;
using SportClubApp.Models;

namespace SportClubApp.Services
{
    public sealed class PayrollService
    {
        public List<ReportRow> GetAdminReport()
        {
            var report = new List<ReportRow>();

            using (var connection = Db.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    a.full_name AS employee,
    COUNT(ar.id) AS sales_count,
    SUM(ISNULL(m.total_amount, 0)) AS sales_amount,
    SUM(ISNULL(m.total_amount, 0)) * 0.05 AS salary_accrued
FROM dbo.admin_report ar
JOIN dbo.administrator a ON a.id = ar.admin_id
JOIN dbo.membership m ON m.id = ar.membership_id
GROUP BY a.full_name
ORDER BY a.full_name;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        report.Add(new ReportRow
                        {
                            Employee = reader.GetString(reader.GetOrdinal("employee")),
                            SalesCount = reader.GetInt32(reader.GetOrdinal("sales_count")),
                            SalesAmount = reader.GetDecimal(reader.GetOrdinal("sales_amount")),
                            SalaryAccrued = reader.GetDecimal(reader.GetOrdinal("salary_accrued"))
                        });
                    }
                }
            }

            return report;
        }
    }
}
