using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace IdentityDotNetTotor.Entities
{
    public class ApplicationRole:IdentityRole
    {
        [MaxLength(100)]
        public string? Description { get; set; }
    }
}
