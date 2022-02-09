using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EspraAPI.Identity;
using EspraAPI.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Sentry;
using MongoDB.Driver;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

#if DEBUG
var connectionString = builder.Configuration["IDENTITY:DEV"];
#else
var connectionString = builder.Configuration["IDENTITY:LIVE"];
#endif

// Sentry
builder.WebHost.UseSentry(builder.Configuration["SENTRY:DNS"]);

// Add services to the container.
builder.Services.AddDbContext<AuthenticationDbContent>(options => options.UseSqlServer(connectionString));

// Identity
builder.Services.AddIdentity<AuthenticationUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthenticationDbContent>()
    .AddDefaultTokenProviders();

var ValidAudience = builder.Configuration["JWT:ValidAudience"];
var ValidIssuer = builder.Configuration["JWT:ValidIssuer"];
var Secret = builder.Configuration["JWT:Secret"];

// JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudience = ValidAudience,
            ValidIssuer = ValidIssuer,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret))
        };
    });

builder.Services.AddAuthorization();

JWT.Init(ValidIssuer, ValidAudience, Secret);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Custom Services
builder.Services.AddTransient<JsonService>();

    // 
var url = builder.Configuration["MONGO:DEV_URL"];
IMongoClient mongoClient = new MongoClient(url);

builder.Services.AddSingleton(mongoClient);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseSentryTracing();


#region Route mappings
app.MapPost("api/login", async (UserManager<AuthenticationUser> userManager, CancellationToken token, [FromBody] LoginModel model) =>
{

    var response = new LoginResponse();

    await Task.Run(async () =>
    {

        var user = await userManager.FindByEmailAsync(model.UserName);

        if (user == null)
        {
            response.Message = "User not found";
            return;
        }

        var loginResult = await userManager.CheckPasswordAsync(user, model.Password);

        if (loginResult)
        {
            var roles = await userManager.GetRolesAsync(user);

            response.Message = "Hello Admin";
            response.Token = JWT.GenerateJWT(user.UserName, roles);
        }
        else
        {
            response.Message = "Invalid Login";
        }

    }, token);

    return response;
});

app.MapGet("api/groups", [Authorize(Roles = "Admin")] () =>
{
    return "List of groups, not the data but group names";
});

app.MapPost("api/post/json/{group}", [Authorize(Roles = "Admin")] async (string group, [FromBody] dynamic data, JsonService jsonService, CancellationToken token) =>
{
    return await jsonService.AddAsync(group, data, token) ? Results.Ok() : Results.BadRequest();
});

app.MapGet("api/get/json/{group}", [Authorize(Roles = "Admin,Web")] async (string group, JsonService jsonService) =>
{
    return Results.Ok(await jsonService.GetAsync(group));
});

app.MapPost("api/update/json/{id}", [Authorize(Roles = "Admin")] async (string id, [FromBody] dynamic ndata, JsonService jsonService, CancellationToken token) =>
{
    return (await jsonService.UpdateAsync(id, ndata, token)) ? Results.Ok() : Results.BadRequest();
});

app.MapDelete("api/delete/json", [Authorize(Roles = "Admin")] () =>
{

});
#endregion


using (var scope = app.Services.CreateAsyncScope())
{
    await CreateRoles(scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());

    await CreateDefaultAccount(scope.ServiceProvider.GetRequiredService<UserManager<AuthenticationUser>>(),
        scope.ServiceProvider.GetRequiredService<IConfiguration>());
}

app.Run();


#region Startup configuration
async static Task CreateRoles(RoleManager<IdentityRole> roleManager)
{
    foreach (var role in JWT.ROLES)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole
            {
                Name = role
            });
        }
    }
}

async static Task CreateDefaultAccount(UserManager<AuthenticationUser> userManager, IConfiguration configuration)
{
    if (await userManager.FindByEmailAsync(configuration["DEFAULT:Email"]) == null)
    {
        var admin = new AuthenticationUser
        {
            Email = configuration["DEFAULT:Email"],
            UserName = configuration["DEFAULT:UserName"]
        };

        var createResult = await userManager.CreateAsync(admin, configuration["DEFAULT:Password"]);

        if (!createResult.Succeeded)
        {
            var builder = new StringBuilder();

            builder.AppendLine("Failed to create default user");
            builder.AppendLine("");

            foreach (var error in createResult.Errors)
            {
                builder.AppendLine($"code={error.Code} | {error.Description}");
            }

            SentrySdk.CaptureMessage(builder.ToString(), SentryLevel.Error);
        }
        else
        {
            var rollResult = await userManager.AddToRoleAsync(admin, "Admin");

            if (!rollResult.Succeeded)
            {
                var builder = new StringBuilder();

                builder.AppendLine("Failed to add admin role to default user");
                builder.AppendLine("");

                foreach (var error in rollResult.Errors)
                {
                    builder.AppendLine($"code={error.Code} | {error.Description}");
                }

                SentrySdk.CaptureMessage(builder.ToString(), SentryLevel.Error);
            }
        }
    }
}
#endregion