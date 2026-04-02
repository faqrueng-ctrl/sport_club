namespace SportClubApp.Models
{
    public sealed class TrainingSessionView
    {
        public int SessionId { get; set; }
        public string Workout { get; set; }
        public string Category { get; set; }
        public string Trainer { get; set; }
        public string StartAt { get; set; }
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public int Capacity { get; set; }
        public int ReservedSeats { get; set; }
        public int FreeSeats => Capacity <= 0 ? 0 : Capacity - ReservedSeats;
    }
}
