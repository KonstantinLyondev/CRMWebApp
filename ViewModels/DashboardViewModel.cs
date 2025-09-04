using System;
using System.Collections.Generic;

namespace CRMWebApp.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalClients { get; set; }
        public int TotalDeals { get; set; }
        public int TotalInteractions { get; set; }
        public int OpenDeals { get; set; } 

        public List<RecentInteractionRow> RecentInteractions { get; set; } = new();
    }

    public class RecentInteractionRow
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string ClientName { get; set; }
        public string DealTitle { get; set; } 
        public string Type { get; set; }      
        public string PerformedBy { get; set; } 
    }
}