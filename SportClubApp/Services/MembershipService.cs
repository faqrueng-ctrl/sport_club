using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using SportClubApp.Data;
using SportClubApp.Models;

namespace SportClubApp.Services
{
    public sealed class MembershipService
    {
        private readonly ScheduleService _scheduleService = new ScheduleService();

        public string SellMembership(MembershipBookingRequest request)
        {
            if (request.SessionIds == null || request.SessionIds.Count == 0)
            {
                throw new InvalidOperationException("Нужно выбрать минимум одну тренировку.");
            }

            using (var connection = Db.OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var schema = new SchemaHelper(connection);
                    var memberId = UpsertMember(request, connection, transaction, schema);

                    foreach (var workoutId in request.SessionIds.Distinct())
                    {
                        _scheduleService.LockSeat(workoutId, connection, transaction);
                        InsertMemberWorkout(memberId, workoutId, connection, transaction, schema);
                    }

                    transaction.Commit();
                    return BuildPrintableCard(memberId, request);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static int UpsertMember(MembershipBookingRequest request, SqlConnection connection, SqlTransaction tx, SchemaHelper schema)
        {
            var idCol = schema.FindIdColumn("Members") ?? "Id";
            var nameCol = schema.FindColumn("Members", "FullName", "Name", "MemberName") ?? "Name";
            var phoneCol = schema.FindColumn("Members", "Phone", "PhoneNumber");
            var emailCol = schema.FindColumn("Members", "Email", "Mail");
            var startCol = schema.FindColumn("Members", "MembershipStartDate", "StartDate", "ValidFrom");
            var endCol = schema.FindColumn("Members", "MembershipEndDate", "EndDate", "ValidTo");

            var existingId = FindExistingMemberId(connection, tx, schema, idCol, phoneCol, emailCol, request);
            if (existingId.HasValue)
            {
                var updates = new List<string> {$"{SchemaHelper.Q(nameCol)} = @fullName"};
                if (phoneCol != null) updates.Add($"{SchemaHelper.Q(phoneCol)} = @phone");
                if (emailCol != null) updates.Add($"{SchemaHelper.Q(emailCol)} = @email");
                if (startCol != null) updates.Add($"{SchemaHelper.Q(startCol)} = @startDate");
                if (endCol != null) updates.Add($"{SchemaHelper.Q(endCol)} = @endDate");

                using (var cmd = connection.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = $"UPDATE dbo.Members SET {string.Join(", ", updates)} WHERE {SchemaHelper.Q(idCol)} = @id";
                    FillMemberParams(cmd, request);
                    cmd.Parameters.AddWithValue("@id", existingId.Value);
                    cmd.ExecuteNonQuery();
                }

                return existingId.Value;
            }

            var insertCols = new List<string> {SchemaHelper.Q(nameCol)};
            var insertVals = new List<string> {@"@fullName"};
            if (phoneCol != null) { insertCols.Add(SchemaHelper.Q(phoneCol)); insertVals.Add("@phone"); }
            if (emailCol != null) { insertCols.Add(SchemaHelper.Q(emailCol)); insertVals.Add("@email"); }
            if (startCol != null) { insertCols.Add(SchemaHelper.Q(startCol)); insertVals.Add("@startDate"); }
            if (endCol != null) { insertCols.Add(SchemaHelper.Q(endCol)); insertVals.Add("@endDate"); }

            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                cmd.CommandText = $@"INSERT INTO dbo.Members ({string.Join(",", insertCols)})
OUTPUT INSERTED.{SchemaHelper.Q(idCol)}
VALUES ({string.Join(",", insertVals)});";
                FillMemberParams(cmd, request);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        private static int? FindExistingMemberId(SqlConnection connection, SqlTransaction tx, SchemaHelper schema, string idCol, string phoneCol, string emailCol, MembershipBookingRequest request)
        {
            if (phoneCol == null && emailCol == null) return null;

            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                var where = new List<string>();
                if (phoneCol != null)
                {
                    where.Add($"{SchemaHelper.Q(phoneCol)} = @phone");
                    cmd.Parameters.AddWithValue("@phone", request.ClientPhone ?? string.Empty);
                }
                if (emailCol != null)
                {
                    where.Add($"{SchemaHelper.Q(emailCol)} = @email");
                    cmd.Parameters.AddWithValue("@email", (object)request.ClientEmail ?? DBNull.Value);
                }

                cmd.CommandText = $"SELECT TOP(1) {SchemaHelper.Q(idCol)} FROM dbo.Members WHERE {string.Join(" OR ", where)}";
                var value = cmd.ExecuteScalar();
                if (value == null || value == DBNull.Value) return null;
                return Convert.ToInt32(value);
            }
        }

        private static void FillMemberParams(SqlCommand cmd, MembershipBookingRequest request)
        {
            cmd.Parameters.AddWithValue("@fullName", request.ClientFullName);
            cmd.Parameters.AddWithValue("@phone", (object)request.ClientPhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object)request.ClientEmail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@startDate", request.ValidFrom.Date);
            cmd.Parameters.AddWithValue("@endDate", request.ValidTo.Date);
        }

        private static void InsertMemberWorkout(int memberId, int workoutId, SqlConnection connection, SqlTransaction tx, SchemaHelper schema)
        {
            var memberCol = schema.FindColumn("MemberWorkouts", "MemberId") ?? "MemberId";
            var workoutCol = schema.FindColumn("MemberWorkouts", "WorkoutId") ?? "WorkoutId";
            var dateCol = schema.FindColumn("MemberWorkouts", "CreatedAt", "BookingDate", "AssignedAt");

            using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = tx;
                var cols = new List<string> {SchemaHelper.Q(memberCol), SchemaHelper.Q(workoutCol)};
                var vals = new List<string> {"@memberId", "@workoutId"};
                if (dateCol != null)
                {
                    cols.Add(SchemaHelper.Q(dateCol));
                    vals.Add("SYSDATETIME()");
                }

                cmd.CommandText = $"INSERT INTO dbo.MemberWorkouts ({string.Join(",", cols)}) VALUES ({string.Join(",", vals)});";
                cmd.Parameters.AddWithValue("@memberId", memberId);
                cmd.Parameters.AddWithValue("@workoutId", workoutId);
                cmd.ExecuteNonQuery();
            }
        }

        private static string BuildPrintableCard(int memberId, MembershipBookingRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("КЛИЕНТСКАЯ КАРТА СПОРТИВНОГО КЛУБА");
            sb.AppendLine($"ID клиента: {memberId}");
            sb.AppendLine($"Клиент: {request.ClientFullName}");
            sb.AppendLine($"Контакты: {request.ClientPhone}, {request.ClientEmail}");
            sb.AppendLine($"Период абонемента: {request.ValidFrom:dd.MM.yyyy} - {request.ValidTo:dd.MM.yyyy}");
            sb.AppendLine($"Выбрано тренировок: {request.SessionIds.Count}");
            sb.AppendLine("Статус: место забронировано");
            return sb.ToString();
        }
    }
}
