using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EspraAPI.Identity
{
    public class AuthenticationDbContent : IdentityDbContext<AuthenticationUser>
    {
        public AuthenticationDbContent(DbContextOptions<AuthenticationDbContent> options) : base(options)
        { 
        }
    }
}
