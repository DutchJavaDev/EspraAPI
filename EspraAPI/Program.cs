using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EspraAPI.Identity;
using EspraAPI.Service;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Sentry;
using MongoDB.Driver;
using EspraAPI;
using static EspraAPI.Configuration.ContentMiddleware;
using EspraAPI.Handlers;

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

Util.Init(ValidIssuer, ValidAudience, Secret);

// Custom Services
builder.Services.AddTransient<AuthenticationService>();
builder.Services.AddTransient<GroupService>();
builder.Services.AddTransient<JsonService>();
builder.Services.AddTransient<FileService>();

var url = builder.Configuration["MONGO:DEV_URL"];
IMongoClient mongoClient = new MongoClient(url);

builder.Services.AddSingleton(mongoClient);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
app.Use(UploadFilter);

var json = "application/json";
var formFile = "multipart/form-data";

#region Route mappings
app.MapPost("api/login", AuthHandler.Login)
.Accepts<LoginModel>(json)
.WithDisplayName("Login route");

app.MapGet("api/get/group/{groupId}", GroupHandler.GetGroupInfo)
.WithDisplayName("Get group info");

app.MapPost("api/post/json/{group}", JsonHandler.PostJson)
.Accepts<string>(json)
.WithDisplayName("New jsondata entry");

app.MapGet("api/get/json/groupId/{group}", JsonHandler.GetJsonByGroupId)
.Accepts<string>(json)
.WithDisplayName("Get all data grouped by groupId");

app.MapGet("api/get/json/id/{id}", JsonHandler.GetJsonById).
 Accepts<string>(json)
.WithDisplayName("Get a jsondata by its documentId");

app.MapPost("api/update/json/{id}", JsonHandler.UpdateJsonById)
.Accepts<dynamic>(json)
.WithDisplayName("Update an existing jsondata by its Id");

app.MapDelete("api/delete/json/{id}", JsonHandler.DeleteJsonById)
.Accepts<dynamic>(json)
.WithDisplayName("Delete jsondata by its Id");

app.MapPost("api/post/document/{group}", FileHandler.PostDocument)
.Accepts<IFormFile>(formFile)
.WithDisplayName("Upload a document");

app.MapPost("api/post/image/{group}", FileHandler.PostImage)
.Accepts<IFormFile>(formFile)
.WithDisplayName("Upload a image");

app.MapGet("api/get/document/{id}", FileHandler.GetDocumentById);

app.MapGet("api/get/image/{id}", FileHandler.GetImageById);
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
    foreach (var role in Util.ROLES)
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