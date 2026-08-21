using DATA.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;

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

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "luftborn", Version = "v1" });
});

// Add services to the container.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
