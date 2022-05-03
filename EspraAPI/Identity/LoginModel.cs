using System.ComponentModel.DataAnnotations;

namespace EspraAPI.Identity
{
    public class LoginModel
    {
        [Required(ErrorMessage = "Email is required")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Email) && 
                   !string.IsNullOrEmpty(Password) &&
                   Email.Length > 1 &&
                   Password.Length > 4;
        }
    }
}
