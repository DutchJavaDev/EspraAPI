using Microsoft.AspNetCore.Mvc;
using EspraAPI.Service;
using EspraAPI.Identity;

namespace EspraAPI.Handlers
{
    public static class AuthHandler
    {

        static AuthHandler()
        {
            // Init
        }

        public static async Task<IResult> Login(AuthenticationService authentication, [FromBody] LoginModel model, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var loginResult = await authentication.Login(model);

            return loginResult.Success ? Results.Ok(loginResult) : Results.BadRequest(loginResult);
        }
    }
}
