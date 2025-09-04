using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMWebApp.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required, MaxLength(32)]
        public string Action { get; set; } = string.Empty; 

        public int? ClientId { get; set; }
        public int? DealId { get; set; }
        public int? InteractionId { get; set; }

        public string? UserId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ApplicationUser? User { get; set; }
        public Client? Client { get; set; }
        public Deal? Deal { get; set; }
        public Interaction? Interaction { get; set; }

        [NotMapped]
        public string EntityType =>
            ClientId.HasValue ? "Client" :
            DealId.HasValue ? "Deal" :
            InteractionId.HasValue ? "Interaction" : "?";

        [NotMapped]
        public int EntityAnyId =>
            ClientId ?? DealId ?? InteractionId ?? 0;
    }
}
