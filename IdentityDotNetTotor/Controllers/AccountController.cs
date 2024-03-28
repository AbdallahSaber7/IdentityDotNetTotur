using IdentityDotNetTotor.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace IdentityDotNetTotor.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public AccountController(UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }
        //Register New User
        [HttpPost("register")]
        public async Task<IActionResult> Register(DTO.RegisterDTO registerDTO)
        {
            if(ModelState.IsValid)
            {
                ApplicationUser user = new ApplicationUser
                {
                    Email = registerDTO.Email,
                    UserName = registerDTO.Email,
                    FirstName = registerDTO.FirstName,
                    LastName = registerDTO.LastName,
                    Address = registerDTO.Address
                };
                IdentityResult result = await userManager.CreateAsync(user, registerDTO.Password);
                if (result.Succeeded)
                {
                    /*bool isPersistent: If set to true, the sign-in cookie will be persistent across browser sessions. If false, the cookie will be session-based and disappear when the browser is closed.
                     string authenticationMethod (Optional): This parameter is optional and can be used to specify the authentication method used. It’s useful for logging and auditing purposes. For example, you might specify “Password” or “TwoFactor” here.*/
                    await signInManager.SignInAsync(user, isPersistent: false);
                    return Ok(new { message = "Registered successfully.", id = user.Id });
                }
                var errors = result.Errors.Select(e=>e.Description).ToList();
                return BadRequest(new {Errors=errors});

            }
            return BadRequest(registerDTO);
        }
        //log in New User
        [HttpPost("LogIn")]
        public async Task<IActionResult> LogIn(DTO.LogInDTO logInDTO)
        {
            if (ModelState.IsValid)
            {
                #region the first way using userManager
                /* //1-check userName
                ApplicationUser user=await userManager.FindByNameAsync(userDTO.UserName);
                if (user != null)
                {
                    //2-check password
                    bool found=await userManager.CheckPasswordAsync(user,userDTO.Password);
                    if (found)
                    {
                 */
                #endregion
                // the second way uding signInManager
                Microsoft.AspNetCore.Identity.SignInResult result = await signInManager.PasswordSignInAsync
                    (logInDTO.Email, logInDTO.Password, logInDTO.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    return Ok("Logged in successfully.");
                }
                if (result.RequiresTwoFactor)
                {
                    return BadRequest("reguire Two factor?");
                }
                if(result.IsLockedOut)
                {
                    return BadRequest("the account locked out ");
                }
               ModelState.AddModelError(string.Empty, "Invalid account");
               return BadRequest(logInDTO);                
                
            }
            return BadRequest(logInDTO);

        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return Ok("Logged out successfully.");
        }

    }

    
}
