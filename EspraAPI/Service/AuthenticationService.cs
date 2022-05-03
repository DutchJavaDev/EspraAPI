using EspraAPI.Identity;
using Microsoft.AspNetCore.Identity;

namespace EspraAPI.Service
{
    public class AuthenticationService
    {
        UserManager<AuthenticationUser> UserManager { get; set; }
        public AuthenticationService(UserManager<AuthenticationUser> userManager)
        {
            UserManager = userManager;
        }

        public async Task<LoginResponse> Login(LoginModel loginModel)
        {
            if(!loginModel.IsValid())
                return new LoginResponse 
                {
                    Success = false,
                    Message = "InValid form"
                };

            var user = await UserManager.FindByEmailAsync(loginModel.Email);

            if (user == null)
                return new LoginResponse 
                { 
                    Success = false, 
                    Message = "User not found" 
                };

            var loginResult = await UserManager.CheckPasswordAsync(user, loginModel.Password);

            if (loginResult)
            {
                return new LoginResponse
                {
                    Success = true,
                    Message = "Authenticated successfully",
                    Token = Util.GenerateJWT(user.UserName, await UserManager.GetRolesAsync(user))
                };
            }

            return new LoginResponse 
            {
                Success = false,
                Message = "Failed to login"
            };
        }
    }
}
