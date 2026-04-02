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
            {
                var schema = new SchemaHelper(connection);

                var wId = schema.FindIdColumn("Workouts") ?? "Id";
                var wName = schema.FindColumn("Workouts", "Name", "WorkoutName", "Title") ?? wId;
                var wCategoryId = schema.FindColumn("Workouts", "CategoryId", "WorkoutCategoryId");
                var wTrainerId = schema.FindColumn("Workouts", "TrainerId");
                var wPrice = schema.FindColumn("Workouts", "Price", "Cost", "Amount");
                var wStart = schema.FindColumn("Workouts", "StartAt", "StartTime", "WorkoutDateTime", "ScheduledAt");
                var wDuration = schema.FindColumn("Workouts", "DurationMinutes", "Duration", "LengthMinutes");
                var wCapacity = schema.FindColumn("Workouts", "Capacity", "MaxMembers", "MaxParticipants", "Spots");

                var cId = schema.FindIdColumn("WorkoutCategories");
                var cName = schema.FindColumn("WorkoutCategories", "Name", "CategoryName", "Title");

                var tId = schema.FindIdColumn("Trainers");
                var tName = schema.FindColumn("Trainers", "FullName", "Name", "TrainerName");
                var tFirst = schema.FindColumn("Trainers", "FirstName");
                var tLast = schema.FindColumn("Trainers", "LastName");

                var mwWorkoutId = schema.FindColumn("MemberWorkouts", "WorkoutId");

                var categoryJoin = wCategoryId != null && cId != null
                    ? $"LEFT JOIN dbo.WorkoutCategories wc ON w.{SchemaHelper.Q(wCategoryId)} = wc.{SchemaHelper.Q(cId)}"
                    : string.Empty;

                var trainerJoin = wTrainerId != null && tId != null
                    ? $"LEFT JOIN dbo.Trainers t ON w.{SchemaHelper.Q(wTrainerId)} = t.{SchemaHelper.Q(tId)}"
                    : string.Empty;

                var reservedSubQuery = mwWorkoutId != null
                    ? $"(SELECT COUNT(*) FROM dbo.MemberWorkouts mw WHERE mw.{SchemaHelper.Q(mwWorkoutId)} = w.{SchemaHelper.Q(wId)})"
                    : "0";

                var categorySelect = cName != null ? $"wc.{SchemaHelper.Q(cName)}" : "N''";
                var trainerSelect = tName != null
                    ? $"t.{SchemaHelper.Q(tName)}"
                    : (tFirst != null && tLast != null
                        ? $"CONCAT(t.{SchemaHelper.Q(tFirst)}, N' ', t.{SchemaHelper.Q(tLast)})"
                        : "N''");

                var priceSelect = wPrice != null ? $"TRY_CAST(w.{SchemaHelper.Q(wPrice)} AS decimal(18,2))" : "0";
                var durationSelect = wDuration != null ? $"TRY_CAST(w.{SchemaHelper.Q(wDuration)} AS int)" : "0";
                var startSelect = wStart != null ? $"CONVERT(nvarchar(30), w.{SchemaHelper.Q(wStart)}, 104)" : "N''";
                var capacitySelect = wCapacity != null ? $"TRY_CAST(w.{SchemaHelper.Q(wCapacity)} AS int)" : "0";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = $@"
SELECT
    TRY_CAST(w.{SchemaHelper.Q(wId)} AS int) AS session_id,
    CAST(w.{SchemaHelper.Q(wName)} AS nvarchar(200)) AS workout_name,
    CAST({categorySelect} AS nvarchar(200)) AS category_name,
    CAST({trainerSelect} AS nvarchar(200)) AS trainer_name,
    CAST({startSelect} AS nvarchar(40)) AS start_at,
    {priceSelect} AS price,
    {durationSelect} AS duration_minutes,
    {capacitySelect} AS capacity,
    {reservedSubQuery} AS reserved_seats
FROM dbo.Workouts w
{categoryJoin}
{trainerJoin}
ORDER BY w.{SchemaHelper.Q(wName)};";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new TrainingSessionView
                            {
                                SessionId = reader["session_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["session_id"]),
                                Workout = Convert.ToString(reader["workout_name"]),
                                Category = Convert.ToString(reader["category_name"]),
                                Trainer = Convert.ToString(reader["trainer_name"]),
                                StartAt = Convert.ToString(reader["start_at"]),
                                Price = reader["price"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["price"]),
                                DurationMinutes = reader["duration_minutes"] == DBNull.Value ? 0 : Convert.ToInt32(reader["duration_minutes"]),
                                Capacity = reader["capacity"] == DBNull.Value ? 0 : Convert.ToInt32(reader["capacity"]),
                                ReservedSeats = reader["reserved_seats"] == DBNull.Value ? 0 : Convert.ToInt32(reader["reserved_seats"])
                            });
                        }
                    }
                }
            }

            return result;
        }

        public void LockSeat(int workoutId, SqlConnection connection, SqlTransaction transaction)
        {
            var schema = new SchemaHelper(connection);
            var wId = schema.FindIdColumn("Workouts") ?? "Id";
            var cap = schema.FindColumn("Workouts", "Capacity", "MaxMembers", "MaxParticipants", "Spots");
            var mwWorkoutId = schema.FindColumn("MemberWorkouts", "WorkoutId");

            if (cap == null || mwWorkoutId == null)
            {
                return;
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = $@"
DECLARE @capacity int;
DECLARE @reserved int;
SELECT @capacity = TRY_CAST({SchemaHelper.Q(cap)} AS int)
FROM dbo.Workouts WITH (UPDLOCK, ROWLOCK)
WHERE {SchemaHelper.Q(wId)} = @workoutId;

SELECT @reserved = COUNT(*)
FROM dbo.MemberWorkouts WITH (UPDLOCK, HOLDLOCK)
WHERE {SchemaHelper.Q(mwWorkoutId)} = @workoutId;

IF (@capacity IS NOT NULL AND @capacity > 0 AND @reserved >= @capacity)
    THROW 51000, N'Свободных мест больше нет: блокировка не выполнена.', 1;";
                command.Parameters.AddWithValue("@workoutId", workoutId);
                command.ExecuteNonQuery();
            }
        }
    }
}
