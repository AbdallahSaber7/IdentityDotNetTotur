using IdentityDotNetTotor.DTO;
using IdentityDotNetTotor.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.Data;
using System.Security.Claims;

namespace IdentityDotNetTotor.Controllers
{    
    //1- [Authorize(Roles = "Admin,Moderator")]//The actions in this controller are accessible only to those users who are members of either the Admin or Moderator role.
    //2-users must be members of both the Admin and Moderator roles.
    //[Authorize(Roles = "Admin")]
    //[Authorize(Roles = "Moderator")]
    //[Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdministrationController : ControllerBase
    {
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public AdministrationController(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
        }
        [HttpPost("CreateRole")]
        [Authorize(Policy = "DeleteandCreateRolePolicy")]
        public async Task<IActionResult> CreateRole(CreateRoleDTO createRoleDTO)
        {
            if (ModelState.IsValid)
            {
                bool roleExist = await roleManager.RoleExistsAsync(createRoleDTO?.RoleName);
                if (roleExist)
                {
                    ModelState.AddModelError(String.Empty, "Role Already Exists");
                    return BadRequest(ModelState);  
                }
                else
                {
                    ApplicationRole identityRole = new ApplicationRole
                    {
                        Name = createRoleDTO.RoleName,
                        Description = createRoleDTO.Description
                    };
                    IdentityResult result = await roleManager.CreateAsync(identityRole);
                    if (result.Succeeded)
                    {
                        return Ok(new { message = "Role Created successfully", identityRole });
                    }
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { Errors = errors });
                }

            }
            return BadRequest(new { Model = createRoleDTO, ModelState });
        }
        [HttpDelete("DeleteRole")]
        [Authorize(Policy = "DeleteandCreateRolePolicy")]
        public async Task<IActionResult> DeleteRole(string roleName)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var role = await roleManager.FindByNameAsync(roleName);
                    if (role == null)
                    {
                        return BadRequest("Not Found Role");
                    }
                    else
                    {
                        var result = await roleManager.DeleteAsync(role);
                        if (result.Succeeded)
                        {
                            return Ok("Delete Success");
                        }
                        var errors = result.Errors.Select(e => e.Description).ToList();
                        return BadRequest(new { Errors = errors });
                    }

                }
                catch (DbUpdateException ex)
                {
                    return BadRequest(new { ex.Message, Note = "Delete the PARENT row" });
                    throw;
                }
            }
            return BadRequest(roleManager.Roles.ToList());
        }
        [HttpGet("AllRoles")]
        public async Task<IActionResult> AllRoles()
        {
            IEnumerable<IdentityRole> roles = roleManager.Roles.ToList();
            return Ok(roles);
        }
        [HttpGet("EditRole")]
        public async Task<IActionResult> EditRole(string roleName)
        {
            ApplicationRole role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return BadRequest("Not Found Role");
            }
            var editDTO = new EditRoleDTO
            {
                Id = role.Id,
                RoleName = role.Name,
                Description = role.Description,
            };
            editDTO.Users = new List<string>();
            editDTO.Claims = new List<string>();
            // Gets a list of claims associated with the specified role.
            var roleClaims = await roleManager.GetClaimsAsync(role);
            editDTO.Claims = roleClaims.Select(c => c.Value).ToList();
            // Retrieve all the Users
            foreach (var user in userManager.Users.ToList())
            {
                // If the user is in this role, add the username to
                // Users property of EditRoleViewModel. 
                // This model object is then passed to the view for display
                if (await userManager.IsInRoleAsync(user, role.Name))
                {
                    editDTO.Users.Add(user.UserName);
                }
            }

            return Ok(editDTO);
        }
        [HttpPut("EditRole")]
        public async Task<IActionResult> EditRole(EditRoleDTO roleDTO)
        {
            if (ModelState.IsValid)
            {
                var role = await roleManager.FindByNameAsync(roleDTO.RoleName);
                if (role == null)
                {
                    return BadRequest("Not Found Role");
                }
                else
                {
                    //role.Name = roleDTO.RoleName;
                    var result = await roleManager.UpdateAsync(role);
                    if (result.Succeeded)
                    {
                        return Ok(new { Id = role.Id, Name = role.Name });
                    }
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return BadRequest(new { Errors = errors });
                }
            }
            return BadRequest(roleDTO);
        }


        [HttpGet("EditUsersInRole")]
        public async Task<IActionResult> EditUsersInRole(string roleName)
        {
            ApplicationRole role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return BadRequest("Not Found Role");
            }
            var models = new List<UserRoleDTO>();
            foreach (var user in userManager.Users.ToList())
            {
                UserRoleDTO UserRoleDTO = new UserRoleDTO()
                {
                    UserId = user.Id,
                    UserName = user.UserName
                };
                if (await userManager.IsInRoleAsync(user, role.Name))
                {
                    UserRoleDTO.IsSelected = true;

                }
                else { UserRoleDTO.IsSelected = false; }
                models.Add(UserRoleDTO);

            }
            return Ok(models);
        }
        [HttpPut("EditUsersInRole")]
        public async Task<IActionResult> EditUsersInRole(List<UserRoleDTO> models, string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return BadRequest("Not Found Role");
            }
            for (int i = 0; i < models.Count; i++)
            {
                var user = await userManager.FindByIdAsync(models[i].UserId);
                IdentityResult? result;
                if (models[i].IsSelected && !(await userManager.IsInRoleAsync(user, role.Name)))
                {
                    //If IsSelected is true and User is not already in this role, then add the user
                    result = await userManager.AddToRoleAsync(user, role.Name);
                }
                else if (!models[i].IsSelected && await userManager.IsInRoleAsync(user, role.Name))
                {
                    //If IsSelected is false and User is already in this role, then remove the user
                    result = await userManager.RemoveFromRoleAsync(user, role.Name);
                }
                else//if IsSelcted and IsIn , if !IsSelcted and !IsIn
                {
                    //Don't do anything simply continue the loop
                    continue;
                }
                if (result.Succeeded)
                {
                    if (i < (models.Count - 1))
                        continue;
                    else
                        return Ok(new { message = "Updated Success", roleId = roleName });
                }
            }
            return BadRequest(new { message = "Updated Fsilure", roleName = roleName });

        }

        [HttpGet("ManageUserRoles")]
        public async Task<IActionResult> ManageUserRoles(string email)
        {
            //First Fetch the User Information from the Identity database by user Id
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Ok("NotFound");
            }
            //Create a List to Hold all the Roles Information
            var model = new List<UserRolesDTO>();
            foreach (var role in await roleManager.Roles.ToListAsync())
            {
                var UserRolesDTO = new UserRolesDTO
                {
                    RoleName = role.Name,
                };
                //Check if the Role is already assigned to this user
                if (await userManager.IsInRoleAsync(user, role.Name))
                {
                    UserRolesDTO.IsSelected = true;
                }
                else
                {
                    UserRolesDTO.IsSelected = false;
                }
                //Add the userRolesViewModel to the model
                model.Add(UserRolesDTO);
            }
            return Ok(model);
        }
        [HttpPut("ManageUserRoles")]
        public async Task<IActionResult> ManageUserRoles(List<UserRolesDTO> userRolesDTOs,string email)
        {
            //First Fetch the User Information from the Identity database by user Id
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("NotFound");
            }
            //fetch the list of roles the specified user belongs to
            var roles = await userManager.GetRolesAsync(user);
            //Then remove all the assigned roles for this user
            var result = await userManager.RemoveFromRolesAsync(user, roles);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return BadRequest(ModelState);
            }
            List<string> RolesToBeAssigned= userRolesDTOs.Select(r => r.RoleName).ToList() ;
            foreach (string? role in RolesToBeAssigned)
            {
                if(!(roleManager.Roles.ToList().Any(e=>e.Name== role)))
                {
                    return BadRequest("Error on Role names");
                }
                RolesToBeAssigned = userRolesDTOs.Where(x => x.IsSelected).Select(y => y.RoleName).ToList();
            }
            //If At least 1 Role is assigned, Any Method will return true
            if (RolesToBeAssigned.Any())
            {
                //add a user to multiple roles simultaneously
                result = await userManager.AddToRolesAsync(user, RolesToBeAssigned);
                if (!result.Succeeded)
                {
                    ModelState.AddModelError("", "Cannot Add Selected Roles to User");
                    return BadRequest(ModelState);
                }
                return Ok(new { Message = $"Added Roles to {email} Successfully" });

            }
            return BadRequest(new { ModelState ,Message="Add Correct data"});
        }

        
        [HttpGet("ListUsers")]
        public IActionResult ListUsers()
        {
            var users = userManager.Users;
            return Ok(users);
        }

        [HttpGet("EditUser")]
        public async Task<IActionResult> EditUser(string UserEmail)
        {
            var user = await userManager.FindByEmailAsync(UserEmail);
            if (user == null)
            {
                return NotFound($"User email = {UserEmail} Not Found");
            }
            // GetClaimsAsync retunrs the list of user Claims
            var userClaims = await userManager.GetClaimsAsync(user);
            // GetRolesAsync returns the list of user Roles
            var userRoles = await userManager.GetRolesAsync(user);
            //Store all the information in the EditUserViewModel instance
            var model = new EditUserDTO
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Claims = userClaims.Select(c => c.Value).ToList(),
                Roles = userRoles
            };
            return Ok(model);
        }
        [HttpPut("EditUser")]
        public async Task<IActionResult> EditUser(EditUserDTO model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound($"User with Id = {model.Id} cannot be found");
            }
            else
            {
                user.Email = model.Email;
                user.UserName = model.UserName;
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                //UpdateAsync Method will update the user data in the AspNetUsers Identity table
                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return Ok("Update Success");
                }
                else
                {
                    //In case any error show the model validation error
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                return BadRequest(ModelState);

            }

        }
        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser(string UserEmail)
        {
            var user = await userManager.FindByEmailAsync(UserEmail);
            if (user == null)
            {
                return NotFound($"User email = {UserEmail} Not Found");
            }
            else
            {
                
                var result = await userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Ok("Delete Success");
                }
                else
                {
                    //In case any error show the model validation error
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                return BadRequest(ModelState);

            }

        }

        [HttpGet("ManageUserClaims")]
        public async Task<IActionResult> ManageUserClaims(string email)
        {
            //First, fetch the User Details Based on the UserId
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return NotFound("NotFound");
            }
            //Create UserClaimDTO Instance
            var model = new UserClaimsDTO
            {
                UserEmail = email
            };
            // UserManager service GetClaimsAsync method gets all the current claims of the user
            var existingUserClaims = await userManager.GetClaimsAsync(user);
            // Loop through each claim we have in our application
            // Call the GetAllClaims Static Method ClaimsStore Class
            foreach (Claim claim in ClaimsStore.GetAllClaims())
            {
                //Create an Instance of UserClaim class
                UserClaim userClaim = new UserClaim
                {
                    ClaimType = claim.Type
                };
                // If the user has the claim, set IsSelected property to true, so the checkbox
                // next to the claim is checked on the UI
                if (existingUserClaims.Any(c => c.Type == claim.Type))
                {
                    userClaim.IsSelected = true;
                }
                //By default the IsSelected is False, no need to set as false
                //Add the userClaim to UserClaimsViewModel Instance 
                model.Cliams.Add(userClaim);
            }
            return Ok(model);
        }

        [HttpPut("ManageUserClaims")]
        public async Task<IActionResult> ManageUserClaims(UserClaimsDTO model)
        {
            //First fetch the User Details
            var user = await userManager.FindByEmailAsync(model.UserEmail);

            if (user == null)
            {
                return NotFound("NotFound");
            }

            // Get all the user existing claims and delete them
            var claims = await userManager.GetClaimsAsync(user);
            var result = await userManager.RemoveClaimsAsync(user, claims);

            if (!result.Succeeded)
            {
                return BadRequest(new { Messaage = "Cannot remove user existing claims", model });
            }

            // Add all the claims that are selected on the UI
            var AllSelectedClaims = model.Cliams.Where(c => c.IsSelected)
                        .Select(c => new Claim(c.ClaimType, c.ClaimType))
                        .ToList();

            //If At least 1 Claim is assigned, Any Method will return true
            if (AllSelectedClaims.Any())
            {
                //add a user to multiple claims simultaneously
                result = await userManager.AddClaimsAsync(user, AllSelectedClaims);

                if (!result.Succeeded)
                {
                    return BadRequest(new { Messaage = "Cannot add selected claims to user", model });

                }
            }

            return Ok(new { model.UserEmail ,Message= "Added Claims Successfully" });
        }
        [HttpGet("ManageRoleClaims")]
        public async Task<IActionResult> ManageRoleClaims(string RoleName)
        {
            //First, fetch the Role Details Based on the RoleId
            var role = await roleManager.FindByNameAsync(RoleName);

            if (role == null)
            {
                return NotFound($"Role with Id = {RoleName} cannot be found");
            }


            //Create RoleClaimsViewModel Instance
            var model = new RoleClaimsDTO
            {
                RoleName = RoleName
            };

            // RoleManager service GetClaimsAsync method gets all the current claims of the role
            var existingRoleClaims = await roleManager.GetClaimsAsync(role);

            // Loop through each claim we have in our application
            // Call the GetAllClaims Static Method ClaimsStore Class
            foreach (Claim claim in ClaimsStore.GetAllClaims())
            {
                //Create an Instance of RoleClaim class
                RoleClaim roleClaim = new RoleClaim
                {
                    ClaimType = claim.Type
                };

                // If the Role has the claim, set IsSelected property to true, so the checkbox
                // next to the claim is checked on the UI
                if (existingRoleClaims.Any(c => c.Type == claim.Type))
                {
                    roleClaim.IsSelected = true;
                }
                //By default, the IsSelected is False, no need to set as false

                //Add the roleClaim to RoleClaimsViewModel Instance 
                model.Claims.Add(roleClaim);
            }

            return Ok(model);
        }
        [HttpPut("ManageRoleClaims")]
        public async Task<IActionResult> ManageRoleClaims(RoleClaimsDTO model)
        {
            //First fetch the Role Details
            var role = await roleManager.FindByIdAsync(model.RoleName);

            if (role == null)
            {
                return NotFound($"Role with name = {model.RoleName} cannot be found");
            }

            // Get all the existing claims of the role
            var claims = await roleManager.GetClaimsAsync(role);


            for (int i = 0; i < model.Claims.Count; i++)
            {
                Claim claim = new Claim(model.Claims[i].ClaimType, model.Claims[i].ClaimType);
                
                IdentityResult? result;

                if (model.Claims[i].IsSelected && !(claims.Any(c => c.Type == claim.Type)))
                {
                    //If IsSelected is true and User is not already in this role, then add the user
                    //result = await _userManager.AddToRoleAsync(user, role.Name);
                    result = await roleManager.AddClaimAsync(role, claim);
                }
                else if (!model.Claims[i].IsSelected && claims.Any(c => c.Type == claim.Type))
                {
                    //If IsSelected is false and User is already in this role, then remove the user
                    result = await roleManager.RemoveClaimAsync(role, claim);
                }
                else
                {
                    //Don't do anything simply continue the loop
                    continue;
                }

                //If you add or remove any user, please check the Succeeded of the IdentityResult
                if (result.Succeeded)
                {
                    if (i < (model.Claims.Count - 1))
                        continue;
                    else
                        return Ok( new { roleName = model.RoleName });
                }
                else
                {
                    ModelState.AddModelError("", "Cannot add or removed selected claims to role");
                    return BadRequest(ModelState);
                }
            }
            return BadRequest();
        }
    }

}
