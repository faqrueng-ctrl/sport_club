namespace SportClubApp.Models
{
    public sealed class TrainingSessionView
    {
        public int SessionId { get; set; }
        public string TrainingType { get; set; }
        public string CoachFullName { get; set; }
        public string StartAt { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
        public int Capacity { get; set; }
        public int ReservedSeats { get; set; }
        public int FreeSeats => Capacity - ReservedSeats;
    }
}
