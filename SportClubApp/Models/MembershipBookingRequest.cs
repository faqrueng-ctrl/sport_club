using System;
using System.Collections.Generic;

namespace SportClubApp.Models
{
    public sealed class MembershipBookingRequest
    {
        public string ClientFullName { get; set; }
        public string ClientPhone { get; set; }
        public string ClientEmail { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public int AdministratorId { get; set; }
        public List<int> SessionIds { get; set; } = new List<int>();
    }
}
