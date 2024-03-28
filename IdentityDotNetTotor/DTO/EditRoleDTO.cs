﻿using System.ComponentModel.DataAnnotations;

namespace IdentityDotNetTotor.DTO
{
    public class EditRoleDTO
    {
        public string Id { get; set; }
        [Required(ErrorMessage = "Role Name is Required")]
        public string RoleName { get; set; }
        public string? Description { get; set; }
        public List<string>? Users { get; set; }
        public List<string>? Claims { get; set; }
    }
}
