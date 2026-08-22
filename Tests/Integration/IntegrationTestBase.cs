using CORE.DTOs.Playlist;
using DATA.DataAccess.Context;
using DATA.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Tests.Integration
{
    /// <summary>
    /// Base class for HTTP-level integration tests. Every test gets a fresh
    /// application (and a fresh in-memory SQLite database) via SetUp/TearDown.
    /// </summary>
    public abstract class IntegrationTestBase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        protected CustomWebApplicationFactory Factory { get; private set; } = null!;
        protected HttpClient Client { get; private set; } = null!;

        [SetUp]
        public async Task IntegrationSetUp()
        {
            Factory = new CustomWebApplicationFactory();
            Client = Factory.CreateClient();

            // Program.cs runs SQL Server migrations at startup, which cannot
            // fully apply to the SQLite test double, so make sure the schema
            // derived from the same EF model exists before each test.
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
        }

        [TearDown]
        public void IntegrationTearDown()
        {
            Client.Dispose();
            Factory.Dispose();
        }

        protected static StringContent AsJson(object? payload) =>
            new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        protected static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response) =>
            await response.Content.ReadFromJsonAsync<T>(JsonOptions);

        /// <summary>Runs assertions against the database backing the API.</summary>
        protected async Task WithDbAsync(Func<AppDbContext, Task> assert)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await assert(db);
        }

        protected async Task<int> SeedSongAsync(string title, string artist)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var song = new Song { Title = title, Artist = artist };
            db.Songs.Add(song);
            await db.SaveChangesAsync();
            return song.Id;
        }

        /// <summary>
        /// Inserts a playlist directly into the database, bypassing the API.
        /// Useful to simulate data owned by other users or pre-existing state.
        /// </summary>
        protected async Task<Playlist> SeedPlaylistAsync(string name, int userId, IEnumerable<int>? songIds = null)
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var songs = songIds is null
                ? []
                : await db.Songs.Where(s => songIds.Contains(s.Id)).ToListAsync();
            var playlist = new Playlist
            {
                Name = name,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                Songs = songs.ToList()
            };

            db.Playlists.Add(playlist);
            await db.SaveChangesAsync();
            return playlist;
        }

        /// <summary>Creates a playlist through the API and returns the created DTO.</summary>
        protected async Task<PlaylistDto> CreatePlaylistViaApiAsync(string name, IEnumerable<int>? songIds = null)
        {
            var response = await Client.PostAsJsonAsync("/api/playlists", new
            {
                name,
                songIds = songIds ?? Array.Empty<int>()
            });

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                Assert.Fail($"POST /api/playlists returned {(int)response.StatusCode}: {body}");
            }

            var dto = await ReadJsonAsync<PlaylistDto>(response)!;
            Assert.That(dto, Is.Not.Null);
            return dto!;
        }
    }
}
