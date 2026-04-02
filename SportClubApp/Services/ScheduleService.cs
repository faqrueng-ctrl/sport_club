using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SportClubApp.Data;
using SportClubApp.Models;

namespace SportClubApp.Services
{
    public sealed class ScheduleService
    {
        public List<TrainingSessionView> GetSchedule()
        {
            var result = new List<TrainingSessionView>();
            using (var connection = Db.OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    s.id AS session_id,
    tt.name AS training_type,
    c.full_name AS coach_name,
    FORMAT(s.start_at, 'dd.MM.yyyy HH:mm') AS start_at,
    s.duration_minutes,
    s.price,
    s.capacity,
    (
        SELECT COUNT(*)
        FROM dbo.booking_session bs
        WHERE bs.session_id = s.id
    ) AS reserved_seats
FROM dbo.training_session s
JOIN dbo.training_type tt ON tt.id = s.training_type_id
JOIN dbo.coach c ON c.id = s.coach_id
WHERE s.is_active = 1
ORDER BY s.start_at;";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new TrainingSessionView
                        {
                            SessionId = reader.GetInt32(reader.GetOrdinal("session_id")),
                            TrainingType = reader.GetString(reader.GetOrdinal("training_type")),
                            CoachFullName = reader.GetString(reader.GetOrdinal("coach_name")),
                            StartAt = reader.GetString(reader.GetOrdinal("start_at")),
                            DurationMinutes = reader.GetInt32(reader.GetOrdinal("duration_minutes")),
                            Price = reader.GetDecimal(reader.GetOrdinal("price")),
                            Capacity = reader.GetInt32(reader.GetOrdinal("capacity")),
                            ReservedSeats = reader.GetInt32(reader.GetOrdinal("reserved_seats"))
                        });
                    }
                }
            }

            return result;
        }

        public void LockSeat(int sessionId, SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE dbo.training_session
SET locked_seats = ISNULL(locked_seats, 0) + 1
WHERE id = @sessionId
  AND (capacity - ISNULL(locked_seats, 0)) > 0;";
                command.Parameters.AddWithValue("@sessionId", sessionId);

                var affected = command.ExecuteNonQuery();
                if (affected == 0)
                {
                    throw new InvalidOperationException("Свободных мест больше нет: блокировка не выполнена.");
                }
            }
        }
    }
}
