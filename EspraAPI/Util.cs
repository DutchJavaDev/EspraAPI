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
        
        // Move this to a external file to be more dynamic ?
        // No need to restart backend when file changes
        public static string[] DOCUMENT_EXTENSIONS = {
            ".txt",
            ".pdf",
            ".docx",
            ".doc", // ?????
            ".html",
            ".css",
            ".js"
        };

        public static string[] IMAGE_EXTENSIONS = {
             ".png",
            ".jpg",
            ".jpeg",
            ".PNG",
            ".JPEG"
        };

        public static string GetImageMIMEType(string extension)
        {
            if (IMAGE_EXTENSIONS.Contains(extension))
            {
                return $"image/{extension[1..].ToLower()}";
            }
            else return "text/plain";
        }

        public static string GetDocumentMIMEType(string extension)
        {
            if (extension == DOCUMENT_EXTENSIONS[0])
                return "text/plain";

            if (extension == DOCUMENT_EXTENSIONS[1])
                return "application/pdf";

            if (extension == DOCUMENT_EXTENSIONS[2])
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            if (extension == DOCUMENT_EXTENSIONS[3])
                return "application/msword";

            if (extension == DOCUMENT_EXTENSIONS[4])
                return "text/html";

            if (extension == DOCUMENT_EXTENSIONS[5])
                return "text/css";

            if (extension == DOCUMENT_EXTENSIONS[6])
                return "text/javascript";

            return "application/octet-stream";
        }

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
                   expires: DateTime.Now.AddSeconds(30),
                   claims: authenticationClaims,
                   signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static string GetUser(string token)
        {
            if (string.IsNullOrEmpty(token))
                return string.Empty;

            var tokenHandler = new JwtSecurityTokenHandler();

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters 
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = ValidAudience,
                    ValidIssuer = ValidIssuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret ?? string.Empty)),

                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validationToken);

                var parsedJwt = (JwtSecurityToken) validationToken;

                return parsedJwt.Claims.First(i => i.Type == ClaimTypes.Name).Value;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
