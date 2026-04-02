using System;

namespace SportClubApp.Models
{
    public sealed class OrderHistoryItem
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
    }
}
