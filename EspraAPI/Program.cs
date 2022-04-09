using EspraAPI.Models;
using EspraAPI.Service;
using Microsoft.AspNetCore.Mvc;
using EspraAPI.Configuration;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration["IDENTITY:DEV"];

// Sentry
//builder.WebHost.UseSentry(builder.Configuration["SENTRY:DNS"]);

// Add services to the container.

// Custom Services
builder.Services.AddTransient<Esp32StorageService>(i => new Esp32StorageService("Server=localhost;Database=iotproject;Uid=root;Pwd=;"));

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

//app.UseAuthentication();
//app.UseAuthorization();
//app.UseSentryTracing();

//var json = "application/json";

var mainDir = Directory.GetCurrentDirectory();

var pageDir = $"{mainDir}/Page/index.html";

var page = File.ReadAllText(pageDir);

#region Route mappings
app.MapGet("/",  () => {
    // custom page for displaying images
    return Results.Extensions.HtmlResponse(page);
});

app.MapGet("api/get", (Esp32StorageService service, CancellationToken token) => 
{
    var snapShots = service.GetAll();

    return Results.Ok(snapShots);
});

app.MapPost("api/post", async (Esp32StorageService service, CancellationToken token, [FromBody] Esp32Model model) =>
{
    if (!model.IsValid)
        return Results.BadRequest("Invalid model");

    return await service.Add(model, token) ? Results.Ok("Model saved") : Results.BadRequest("Error saving model");
});
#endregion


app.Run();


#region Startup configuration

#endregion