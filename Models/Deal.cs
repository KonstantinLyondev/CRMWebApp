using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;

namespace CRMWebApp.Models
{
    public enum DealStatus
    {
        [Display(Name = "In Progress")]
        InProgress,
        [Display(Name = "Successful")]
        Successful,
        [Display(Name = "Failed")]
        Failed
    }

    [Index(nameof(UserId), nameof(IsDeleted))]
    [Index(nameof(Status), nameof(IsDeleted))]
    public class Deal: IOwnedEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Client selection is required.")]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        [ValidateNever]              
        public Client? Client { get; set; }

        [Required(ErrorMessage = "Status is required.")]
        public DealStatus Status { get; set; } = DealStatus.InProgress;

        [Range(0, double.MaxValue, ErrorMessage = "Amount must be 0 or greater.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency must be a 3-letter code (e.g., EUR, USD).")]
        [StringLength(3, ErrorMessage = "Currency code must be 3 characters.")]
        public string Currency { get; set; } = "EUR";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public byte Probability { get; set; } = 0;

        [DataType(DataType.Date)]
        public DateTime? CloseDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public string? UserId { get; set; } 

        [ForeignKey(nameof(UserId))]
        [ValidateNever]
        public ApplicationUser? User { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        [ValidateNever]
        public ApplicationUser? CreatedBy { get; set; }
    }
}
