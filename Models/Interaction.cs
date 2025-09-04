using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRMWebApp.Models
{
    public class Interaction: IOwnedEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Client selection is required.")]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client Client { get; set; } = null!;
        public int? DealId { get; set; }

        [ForeignKey(nameof(DealId))]
        public Deal? Deal { get; set; }

        [Required(ErrorMessage = "Date is required.")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Type is required.")]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Comment { get; set; }

        public string? UserId { get; set; } 

        [ForeignKey(nameof(UserId))]
        public ApplicationUser User { get; set; } = null!;

        public bool IsDeleted { get; set; } = false;
        
        public string? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        [ValidateNever]
        public ApplicationUser? CreatedBy { get; set; }
    }
}
