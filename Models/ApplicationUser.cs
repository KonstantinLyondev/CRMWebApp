using Microsoft.AspNetCore.Identity;

namespace CRMWebApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Company { get; set; }
        public bool IsDeactivated { get; set; }
    }
}


