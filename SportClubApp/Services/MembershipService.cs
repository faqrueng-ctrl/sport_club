using System;
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
                    var clientId = UpsertClient(request, connection, transaction);
                    var membershipId = InsertMembership(request, clientId, connection, transaction);

                    foreach (var sessionId in request.SessionIds.Distinct())
                    {
                        _scheduleService.LockSeat(sessionId, connection, transaction);
                        LinkMembershipToSession(membershipId, sessionId, connection, transaction);
                    }

                    CreatePaymentRecord(membershipId, connection, transaction);
                    CreateAdminReportRow(request.AdministratorId, membershipId, connection, transaction);

                    transaction.Commit();
                    return BuildPrintableCard(membershipId, request);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static int UpsertClient(MembershipBookingRequest request, SqlConnection connection, SqlTransaction tx)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = tx;
                command.CommandText = @"
MERGE dbo.client AS target
USING (SELECT @phone AS phone) AS source
ON target.phone = source.phone
WHEN MATCHED THEN
    UPDATE SET full_name = @fullName, email = @email
WHEN NOT MATCHED THEN
    INSERT (full_name, phone, email) VALUES (@fullName, @phone, @email)
OUTPUT inserted.id;";

                command.Parameters.AddWithValue("@fullName", request.ClientFullName);
                command.Parameters.AddWithValue("@phone", request.ClientPhone);
                command.Parameters.AddWithValue("@email", (object)request.ClientEmail ?? DBNull.Value);

                return (int)command.ExecuteScalar();
            }
        }

        private static int InsertMembership(MembershipBookingRequest request, int clientId, SqlConnection connection, SqlTransaction tx)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = tx;
                command.CommandText = @"
INSERT INTO dbo.membership
(client_id, valid_from, valid_to, sold_by_admin_id, created_at)
OUTPUT INSERTED.id
VALUES
(@clientId, @validFrom, @validTo, @adminId, SYSDATETIME());";

                command.Parameters.AddWithValue("@clientId", clientId);
                command.Parameters.AddWithValue("@validFrom", request.ValidFrom.Date);
                command.Parameters.AddWithValue("@validTo", request.ValidTo.Date);
                command.Parameters.AddWithValue("@adminId", request.AdministratorId);

                return (int)command.ExecuteScalar();
            }
        }

        private static void LinkMembershipToSession(int membershipId, int sessionId, SqlConnection connection, SqlTransaction tx)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = tx;
                command.CommandText = @"
INSERT INTO dbo.booking_session
(membership_id, session_id, booking_status, booked_at)
VALUES
(@membershipId, @sessionId, N'Забронировано', SYSDATETIME());";

                command.Parameters.AddWithValue("@membershipId", membershipId);
                command.Parameters.AddWithValue("@sessionId", sessionId);
                command.ExecuteNonQuery();
            }
        }

        private static void CreatePaymentRecord(int membershipId, SqlConnection connection, SqlTransaction tx)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = tx;
                command.CommandText = @"
INSERT INTO dbo.payment
(membership_id, paid_at, payment_status, transfer_to_accounting_status)
VALUES
(@membershipId, SYSDATETIME(), N'Оплачено', N'Передано в бухгалтерию');";
                command.Parameters.AddWithValue("@membershipId", membershipId);
                command.ExecuteNonQuery();
            }
        }

        private static void CreateAdminReportRow(int adminId, int membershipId, SqlConnection connection, SqlTransaction tx)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = tx;
                command.CommandText = @"
INSERT INTO dbo.admin_report
(admin_id, membership_id, created_at)
VALUES
(@adminId, @membershipId, SYSDATETIME());";

                command.Parameters.AddWithValue("@adminId", adminId);
                command.Parameters.AddWithValue("@membershipId", membershipId);
                command.ExecuteNonQuery();
            }
        }

        private static string BuildPrintableCard(int membershipId, MembershipBookingRequest request)
        {
            var sb = new StringBuilder();
            sb.AppendLine("КЛИЕНТСКАЯ КАРТА СПОРТИВНОГО КЛУБА");
            sb.AppendLine($"№ абонемента: {membershipId}");
            sb.AppendLine($"Клиент: {request.ClientFullName}");
            sb.AppendLine($"Контакты: {request.ClientPhone}, {request.ClientEmail}");
            sb.AppendLine($"Период: {request.ValidFrom:dd.MM.yyyy} - {request.ValidTo:dd.MM.yyyy}");
            sb.AppendLine($"Выбрано тренировок: {request.SessionIds.Count}");
            sb.AppendLine("Статус: Оплачено и забронировано");
            return sb.ToString();
        }
    }
}
