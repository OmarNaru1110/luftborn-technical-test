using API.Services;
using CORE.Services;
using CORE.Services.IServices;
using DATA.DataAccess.Context;
using DATA.DataAccess.Repositories.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

DotNetEnv.Env.Load();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddDbContext<AppDbContext>(options =>
options.UseSqlServer(
    Environment.GetEnvironmentVariable("SQLServer_ConnectionString") ?? builder.Configuration.GetConnectionString("SQLServer"),
    sqlOptions => sqlOptions
    .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)
    .EnableRetryOnFailure()
    )
);

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Reject numeric strings ("10") for number fields, and make the OpenAPI
// generator emit plain "type": "integer" schemas instead of integer|string.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    });
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;

    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = "luftborn";
        return Task.CompletedTask;
    });
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<ISongService, SongService>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IPlaylistService, PlaylistService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Wait for DB to be ready and apply migrations
        if (context.Database.IsRelational())
        {
            context.Database.Migrate();
        }

        // Seed data
        if (!context.Songs.Any())
        {
            context.Songs.AddRange(
                new DATA.Models.Song { Title = "Bohemian Rhapsody", Artist = "Queen" },
                new DATA.Models.Song { Title = "Stairway to Heaven", Artist = "Led Zeppelin" },
                new DATA.Models.Song { Title = "Hotel California", Artist = "Eagles" },
                new DATA.Models.Song { Title = "Imagine", Artist = "John Lennon" },
                new DATA.Models.Song { Title = "Smells Like Teen Spirit", Artist = "Nirvana" },
                new DATA.Models.Song { Title = "Sweet Child O' Mine", Artist = "Guns N' Roses" },
                new DATA.Models.Song { Title = "Like a Rolling Stone", Artist = "Bob Dylan" },
                new DATA.Models.Song { Title = "Billie Jean", Artist = "Michael Jackson" },
                new DATA.Models.Song { Title = "Hey Jude", Artist = "The Beatles" },
                new DATA.Models.Song { Title = "Purple Haze", Artist = "Jimi Hendrix" }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}

// Configure the HTTP request pipeline.
app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "v1");

    if (!app.Environment.IsDevelopment())
    {
        options.RoutePrefix = string.Empty;
    }
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
