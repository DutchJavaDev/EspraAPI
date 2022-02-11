using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EspraAPI
{
    public static class Util
    {
        public static readonly string DATE_FORMAT = "MM/dd/yyyy HH:mm:ss";


        public static string[] ROLES = {
            "Admin", // <- read + write
            "Web", // <- read
        };

        private static string? ValidIssuer;
        private static string? ValidAudience;
        private static string? Secret;

        public static void Init(string validIssuer, string validAudience, string secret)
        {
            ValidIssuer = validIssuer;
            ValidAudience = validAudience;
            Secret = secret;
        }

        public static string GenerateJWT(string username, IList<string> roles)
        {
            var authenticationClaims = new List<Claim> {
                new Claim(ClaimTypes.Name, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles)
            {
                authenticationClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret ?? string.Empty));

            var token = new JwtSecurityToken(
                   issuer: ValidIssuer,
                   audience: ValidAudience,
                   expires: DateTime.Now.AddHours(3),
                   claims: authenticationClaims,
                   signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
