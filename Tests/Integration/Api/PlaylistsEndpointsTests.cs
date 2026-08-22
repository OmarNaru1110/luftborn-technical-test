using CORE.DTOs.Playlist;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace Tests.Integration.Api
{
    public class PlaylistsEndpointsTests : IntegrationTestBase
    {
        [Test]
        public async Task CreatePlaylist_WithValidNameAndSongs_ReturnsCreatedAndPersists()
        {
            var songId = await SeedSongAsync("Everlong", "Foo Fighters");

            var response = await Client.PostAsJsonAsync("/api/playlists", new
            {
                name = "Road Trip",
                songIds = new[] { songId }
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var created = await ReadJsonAsync<PlaylistDto>(response)!;
            Assert.That(created, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(created!.Id, Is.GreaterThan(0));
                Assert.That(created.Name, Is.EqualTo("Road Trip"));
                Assert.That(created.UserId, Is.EqualTo(1));
                Assert.That(created.Songs.Select(s => s.Id), Is.EqualTo(new[] { songId }));
            });

            await WithDbAsync(async db =>
            {
                var playlist = await db.Playlists.FindAsync(created.Id);
                Assert.That(playlist, Is.Not.Null);
                Assert.That(playlist!.Name, Is.EqualTo("Road Trip"));
                Assert.That(await db.PlaylistSongs.CountAsync(ps => ps.PlaylistId == created.Id), Is.EqualTo(1));
            });
        }

        [Test]
        public async Task CreatePlaylist_WithUnknownSongIds_IgnoresThemAndCreatesEmptyPlaylist()
        {
            var response = await Client.PostAsJsonAsync("/api/playlists", new
            {
                name = "Ghost Songs",
                songIds = new[] { 99999 }
            });

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
                var created = ReadJsonAsync<PlaylistDto>(response).Result!;
                Assert.That(created, Is.Not.Null);
                Assert.That(created!.Songs, Is.Empty);
            });
        }

        [Test]
        public async Task CreatePlaylist_WithoutName_ReturnsValidationError()
        {
            var response = await Client.PostAsJsonAsync("/api/playlists", new
            {
                songIds = Array.Empty<int>()
            });
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("Name"));
            });
        }

        [Test]
        public async Task CreatePlaylist_NameOver100Characters_ReturnsValidationError()
        {
            var response = await Client.PostAsJsonAsync("/api/playlists", new
            {
                name = new string('x', 101)
            });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task CreatePlaylist_SongIdsAsStrings_RejectedByStrictNumberHandling()
        {
            var response = await Client.PostAsync("/api/playlists",
                AsJson(new { name = "Strict", songIds = new[] { "1" } }));

            // JsonNumberHandling.Strict rejects numeric strings for int fields.
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task GetPlaylist_Existing_ReturnsWithSongs()
        {
            var songId = await SeedSongAsync("Creep", "Radiohead");
            var playlist = await SeedPlaylistAsync("Chill Mix", userId: 1, new[] { songId });

            var response = await Client.GetAsync($"/api/playlists/{playlist.Id}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var dto = await ReadJsonAsync<PlaylistDto>(response)!;

            Assert.Multiple(() =>
            {
                Assert.That(dto, Is.Not.Null);
                Assert.That(dto!.Id, Is.EqualTo(playlist.Id));
                Assert.That(dto!.UserId, Is.EqualTo(1));
                Assert.That(dto!.CreatedAt, Is.Not.EqualTo(default(DateTime)));
                Assert.That(dto!.Songs.Select(s => s.Id), Is.EqualTo(new[] { songId }));
                Assert.That(dto!.Songs.Single().Title, Is.EqualTo("Creep"));
            });
        }

        [Test]
        public async Task GetPlaylist_UnknownId_ReturnsNotFound()
        {
            var response = await Client.GetAsync("/api/playlists/99999");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(body, Does.Contain("Playlist not found").IgnoreCase);
            });
        }

        [Test]
        public async Task GetPlaylists_ReturnsOnlyPlaylistsOfCurrentUser()
        {
            await SeedSongAsync("Everlong", "Foo Fighters");
            var mine = await CreatePlaylistViaApiAsync("Mine");
            await SeedPlaylistAsync("Someone Else's", userId: 2);

            var response = await Client.GetAsync("/api/playlists");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var playlists = await ReadJsonAsync<List<PlaylistDto>>(response)!;

            Assert.Multiple(() =>
            {
                Assert.That(playlists!, Has.Count.EqualTo(1));
                Assert.That(playlists!.Single().Id, Is.EqualTo(mine.Id));
                Assert.That(playlists!.Single().Name, Is.EqualTo("Mine"));
                Assert.That(playlists!.Single().UserId, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task UpdatePlaylist_NewName_PersistsRename()
        {
            var playlist = await SeedPlaylistAsync("Old Name", userId: 1);

            var response = await Client.PutAsJsonAsync($"/api/playlists/{playlist.Id}", new { name = "New Name" });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var dto = await ReadJsonAsync<PlaylistDto>(response)!;
            Assert.That(dto, Is.Not.Null);
            Assert.That(dto!.Name, Is.EqualTo("New Name"));

            await WithDbAsync(async db =>
            {
                var reloaded = await db.Playlists.FindAsync(playlist.Id);
                Assert.That(reloaded!.Name, Is.EqualTo("New Name"));
            });
        }

        [Test]
        public async Task UpdatePlaylist_UnknownId_ReturnsNotFound()
        {
            var response = await Client.PutAsJsonAsync("/api/playlists/99999", new { name = "Whatever" });
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(body, Does.Contain("Playlist not found").IgnoreCase);
            });
        }

        [Test]
        public async Task DeletePlaylist_RemovesPlaylistAndJoinsButNotSongs()
        {
            var songId = await SeedSongAsync("Everlong", "Foo Fighters");
            var playlist = await SeedPlaylistAsync("Doomed", userId: 1, new[] { songId });

            var deleteResponse = await Client.DeleteAsync($"/api/playlists/{playlist.Id}");

            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            var getResponse = await Client.GetAsync($"/api/playlists/{playlist.Id}");
            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            await WithDbAsync(async db =>
            {
                Assert.That(await db.Playlists.CountAsync(p => p.Id == playlist.Id), Is.EqualTo(0));
                Assert.That(await db.PlaylistSongs.CountAsync(ps => ps.PlaylistId == playlist.Id), Is.EqualTo(0),
                    "join rows should be removed by cascade delete");
                Assert.That(await db.Songs.CountAsync(s => s.Id == songId), Is.EqualTo(1),
                    "songs must survive playlist deletion");
            });
        }

        [Test]
        public async Task DeletePlaylist_UnknownId_ReturnsNotFound()
        {
            var response = await Client.DeleteAsync("/api/playlists/99999");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(body, Does.Contain("Playlist not found").IgnoreCase);
            });
        }
    }
}
