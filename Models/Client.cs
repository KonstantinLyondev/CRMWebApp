using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace CRMWebApp.Models
{
    public class Client: IOwnedEntity
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(256)]
        [RegularExpression(@"^[a-z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$",
    ErrorMessage = "Invalid email address.")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\+?[0-9\s\-\(\)]{7,20}$", ErrorMessage = "Invalid phone number.")]
        public string Phone { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "City cannot exceed 50 characters.")]
        public string City { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Company name cannot exceed 100 characters.")]
        public string Company { get; set; } = string.Empty;

        [StringLength(80)]
        public string? Address { get; set; }

        public bool IsVip { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public string? UserId { get; set; } 

        [ForeignKey(nameof(UserId))]
        [ValidateNever]
        public ApplicationUser? User { get; set; }

        [ValidateNever]
        public ICollection<Interaction> Interactions { get; set; } = new List<Interaction>();

        [ValidateNever]
        public ICollection<Deal> Deals { get; set; } = new List<Deal>();

        public string? CreatedById { get; set; }

        [ForeignKey(nameof(CreatedById))]
        [ValidateNever]
        public ApplicationUser? CreatedBy { get; set; }
    }
}
