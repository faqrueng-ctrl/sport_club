using System;
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
            {
                var schema = new SchemaHelper(connection);

                var tId = schema.FindIdColumn("Trainers") ?? "Id";
                var tName = schema.FindColumn("Trainers", "FullName", "Name", "TrainerName");
                var tFirst = schema.FindColumn("Trainers", "FirstName");
                var tLast = schema.FindColumn("Trainers", "LastName");
                var wTrainerId = schema.FindColumn("Workouts", "TrainerId");
                var wId = schema.FindIdColumn("Workouts") ?? "Id";
                var wPrice = schema.FindColumn("Workouts", "Price", "Cost", "Amount");
                var mwWorkoutId = schema.FindColumn("MemberWorkouts", "WorkoutId");

                if (wTrainerId == null || mwWorkoutId == null)
                {
                    return report;
                }

                var trainerExpr = tName != null
                    ? $"t.{SchemaHelper.Q(tName)}"
                    : (tFirst != null && tLast != null
                        ? $"CONCAT(t.{SchemaHelper.Q(tFirst)}, N' ', t.{SchemaHelper.Q(tLast)})"
                        : "N'Тренер'");

                var priceExpr = wPrice != null ? $"TRY_CAST(w.{SchemaHelper.Q(wPrice)} AS decimal(18,2))" : "0";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
SELECT
    CAST({trainerExpr} AS nvarchar(200)) AS employee,
    COUNT(*) AS sales_count,
    SUM({priceExpr}) AS sales_amount,
    SUM({priceExpr}) * 0.10 AS salary_accrued
FROM dbo.MemberWorkouts mw
JOIN dbo.Workouts w ON mw.{SchemaHelper.Q(mwWorkoutId)} = w.{SchemaHelper.Q(wId)}
JOIN dbo.Trainers t ON w.{SchemaHelper.Q(wTrainerId)} = t.{SchemaHelper.Q(tId)}
GROUP BY {trainerExpr}
ORDER BY {trainerExpr};";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            report.Add(new ReportRow
                            {
                                Employee = Convert.ToString(reader["employee"]),
                                SalesCount = reader["sales_count"] == DBNull.Value ? 0 : Convert.ToInt32(reader["sales_count"]),
                                SalesAmount = reader["sales_amount"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["sales_amount"]),
                                SalaryAccrued = reader["salary_accrued"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["salary_accrued"])
                            });
                        }
                    }
                }
            }

            return report;
        }
    }
}
