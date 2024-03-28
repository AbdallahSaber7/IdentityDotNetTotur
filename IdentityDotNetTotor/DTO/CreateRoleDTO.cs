using System.ComponentModel.DataAnnotations;

namespace IdentityDotNetTotor.DTO
{
    public class CreateRoleDTO
    {
        [Required]
        [Display(Name = "Role")]
        public string RoleName { get; set; }
        public string? Description { get; set; }
    }

}
