using System.ComponentModel.DataAnnotations;

namespace EspraAPI.Identity
{
    public class LoginModel
    {
        [Required(ErrorMessage = "UserName is required")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(UserName) && 
                   !string.IsNullOrEmpty(Password) &&
                   UserName.Length > 1 &&
                   Password.Length > 4;
        }
    }
}
