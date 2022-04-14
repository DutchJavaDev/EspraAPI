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
using EspraAPI;
using static EspraAPI.Configuration.ContentMiddleware;

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
app.MapPost("api/login", async (AuthenticationService authentication, CancellationToken token, [FromBody] LoginModel model) =>
{
    var loginResult = await authentication.Login(model);

    return loginResult.Success ? Results.Ok(loginResult) : Results.BadRequest(loginResult);

}).Accepts<LoginModel>(json)
.Produces<LoginResponse>(StatusCodes.Status200OK)
.Produces<LoginResponse>(StatusCodes.Status400BadRequest)
.WithDisplayName("Login route");

app.MapPost("api/post/json/{group}", [Authorize(Roles = "Admin")] async (string group, [FromBody] dynamic data, JsonService jsonService, CancellationToken token) =>
{
    return await jsonService.AddAsync(group, data, token) ? Results.Ok() : Results.BadRequest();
}).Accepts<dynamic>(json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithDisplayName("New jsondata entry");


app.MapGet("api/get/json/groupId/{group}", [Authorize(Roles = "Admin")] async (string group, CancellationToken token, JsonService jsonService) =>
{
    return await jsonService.GetCollectionAsync(group, token);
}).Accepts<string>(json)
.Produces<IList<JsonData>>(StatusCodes.Status200OK)
.WithDisplayName("Get all data grouped by groupId");

app.MapGet("api/get/json/id/{id}", [Authorize(Roles = "Admin")] async (string id, CancellationToken token, JsonService jsonService) =>
{
    var result = await jsonService.GetByIdAsync(id, token);

    return ! (result == null) ? Results.Ok(result) : Results.BadRequest();
}).Accepts<string>(json)
.Produces<JsonData>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithDisplayName("Get a jsondata by its documentId");


app.MapPost("api/update/json/{id}", [Authorize(Roles = "Admin")] async (string id, [FromBody] dynamic ndata, JsonService jsonService, CancellationToken token) =>
{
    return (await jsonService.UpdateAsync(id, ndata, token)) ? Results.Ok() : Results.BadRequest();
}).Accepts<dynamic>(json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithDisplayName("Update an existing jsondata by its Id");


app.MapDelete("api/delete/json/{id}", [Authorize(Roles = "Admin")] async (string id, CancellationToken token, JsonService jsonService) =>
{
    return (await jsonService.DeleteAsync(id, token)) ? Results.Ok() : Results.BadRequest();
}).Accepts<dynamic>(json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithDisplayName("Delete jsondata by its Id");

app.MapPost("api/post/document/{group}", [Authorize(Roles = "Admin")]  async (string group, HttpRequest request, CancellationToken token, FileService fileService) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest();

    var form = await request.ReadFormAsync();

    var document = form.Files.First(i => i != null && i.Length > 0);

    if (document is null)
        return Results.BadRequest("Empty request");

    var documentExtension = Path.GetExtension(document.FileName);
    
    if(!Util.DOCUMENT_EXTENSIONS.Contains(documentExtension))
        return Results.BadRequest("Unsupported file");

    using var stream = new MemoryStream();

    document.CopyTo(stream);

    return await fileService.AddAsync(group, documentExtension, stream.ToArray(), token) ? Results.Ok() : Results.BadRequest();
}).Accepts<IFormFile>(formFile)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithDisplayName("Upload a document");

app.MapPost("api/post/image/{group}", [Authorize(Roles = "Admin")] async (string group, HttpRequest request, CancellationToken token, FileService fileService) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest();

    var form = await request.ReadFormAsync();

    var image = form.Files.First(i => i != null && i.Length > 0);

    if (image is null)
        return Results.BadRequest("Empty request");

    var imageExtension = Path.GetExtension(image.FileName);

    if (!Util.IMAGE_EXTENSIONS.Contains(imageExtension))
        return Results.BadRequest("Unsupported file");

    using var stream = new MemoryStream();

    image.CopyTo(stream);

    return await fileService.AddAsync(group, imageExtension, stream.ToArray(), token) ? Results.Ok() : Results.BadRequest();
}).Accepts<IFormFile>(formFile)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.WithDisplayName("Upload a image");

app.MapGet("api/get/document/{id}", async (string id, FileService fileService, CancellationToken token) => 
{
    var documentData = await fileService.GetDocumentById(id, token);

    return Results.File(documentData.Item1,documentData.Item2);
});

app.MapGet("api/get/image/{id}", async (string id, FileService fileService, CancellationToken token) =>
{
    var imageData = await fileService.GetImageById(id, token);

    return Results.File(imageData.Item1, imageData.Item2);
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