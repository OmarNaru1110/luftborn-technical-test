using CORE.DTOs.Playlist;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Json;

namespace Tests.Integration.Api
{
    public class PlaylistWorkflowTests : IntegrationTestBase
    {
        [Test]
        public async Task FullLifecycle_CreateAddSongsRenameDelete_WorksEndToEnd()
        {
            var song1 = await SeedSongAsync("Everlong", "Foo Fighters");
            var song2 = await SeedSongAsync("Creep", "Radiohead");
            var song3 = await SeedSongAsync("Karma Police", "Radiohead");

            // 1. Create with one song.
            var created = await CreatePlaylistViaApiAsync("Journey", new[] { song1 });
            Assert.That(created.Songs.Select(s => s.Id), Is.EqualTo(new[] { song1 }));

            // 2. Add more songs through the dedicated endpoint.
            var addResponse = await Client.PostAsJsonAsync($"/api/playlists/{created.Id}/songs", new[] { song2, song3 });
            Assert.That(addResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var afterAdd = await ReadJsonAsync<PlaylistDto>(addResponse)!;
            Assert.That(afterAdd!.Songs.Select(s => s.Id), Is.EquivalentTo(new[] { song1, song2, song3 }));

            // 3. Rename.
            var renameResponse = await Client.PutAsJsonAsync($"/api/playlists/{created.Id}", new { name = "Epic Journey" });
            Assert.That(renameResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // 4. Fetch and verify final state.
            var getResponse = await Client.GetAsync($"/api/playlists/{created.Id}");
            Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var fetched = await ReadJsonAsync<PlaylistDto>(getResponse)!;
            Assert.Multiple(() =>
            {
                Assert.That(fetched!.Name, Is.EqualTo("Epic Journey"));
                Assert.That(fetched!.Songs.Count(), Is.EqualTo(3));
            });

            // 5. Delete and confirm it is gone.
            var deleteResponse = await Client.DeleteAsync($"/api/playlists/{created.Id}");
            Assert.That(deleteResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

            var goneResponse = await Client.GetAsync($"/api/playlists/{created.Id}");
            Assert.That(goneResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task AddSongs_NewSongs_PersistsJoinRows()
        {
            var song1 = await SeedSongAsync("Everlong", "Foo Fighters");
            var song2 = await SeedSongAsync("Creep", "Radiohead");
            var playlist = await SeedPlaylistAsync("Starter", userId: 1, new[] { song1 });

            var response = await Client.PostAsJsonAsync($"/api/playlists/{playlist.Id}/songs", new[] { song2 });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            await WithDbAsync(async db =>
            {
                var joinCount = await db.PlaylistSongs.CountAsync(ps => ps.PlaylistId == playlist.Id);
                Assert.That(joinCount, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task AddSongs_SongsAlreadyInPlaylist_ReturnsSuccessWithoutDuplication()
        {
            var songId = await SeedSongAsync("Everlong", "Foo Fighters");
            var playlist = await SeedPlaylistAsync("Dedup", userId: 1, new[] { songId });

            var response = await Client.PostAsJsonAsync($"/api/playlists/{playlist.Id}/songs", new[] { songId });

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var dto = ReadJsonAsync<PlaylistDto>(response).Result!;
                Assert.That(dto!.Songs.Count(s => s.Id == songId), Is.EqualTo(1));
            });

            await WithDbAsync(async db =>
            {
                var joinCount = await db.PlaylistSongs.CountAsync(ps => ps.PlaylistId == playlist.Id);
                Assert.That(joinCount, Is.EqualTo(1), "duplicate adds must not create extra join rows");
            });
        }

        [Test]
        public async Task AddSongs_DuplicateIdsWithinRequest_AddsEachSongOnce()
        {
            var songId = await SeedSongAsync("Creep", "Radiohead");
            var playlist = await SeedPlaylistAsync("Once Only", userId: 1);

            var response = await Client.PostAsJsonAsync($"/api/playlists/{playlist.Id}/songs", new[] { songId, songId, songId });

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            await WithDbAsync(async db =>
            {
                var joinCount = await db.PlaylistSongs.CountAsync(ps => ps.PlaylistId == playlist.Id);
                Assert.That(joinCount, Is.EqualTo(1),
                    "composite primary key on PlaylistSongs must prevent duplicates");
            });
        }

        [Test]
        public async Task AddSongs_MixOfKnownAndUnknownIds_AddsOnlyKnownSongs()
        {
            var songId = await SeedSongAsync("Karma Police", "Radiohead");
            var playlist = await SeedPlaylistAsync("Selective", userId: 1);

            var response = await Client.PostAsJsonAsync($"/api/playlists/{playlist.Id}/songs", new[] { songId, 99999 });

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var dto = ReadJsonAsync<PlaylistDto>(response).Result!;
                Assert.That(dto!.Songs.Select(s => s.Id), Is.EqualTo(new[] { songId }));
            });
        }

        [Test]
        public async Task AddSongs_EmptyList_ReturnsBadRequest()
        {
            var playlist = await SeedPlaylistAsync("Empty Request", userId: 1);

            var response = await Client.PostAsJsonAsync($"/api/playlists/{playlist.Id}/songs", Array.Empty<int>());
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("No songs provided").IgnoreCase);
            });
        }

        [Test]
        public async Task AddSongs_NullBody_ReturnsBadRequest()
        {
            var playlist = await SeedPlaylistAsync("Null Request", userId: 1);

            var response = await Client.PostAsync($"/api/playlists/{playlist.Id}/songs", AsJson(null));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(body, Does.Contain("No songs provided").IgnoreCase);
            });
        }

        [Test]
        public async Task AddSongs_UnknownPlaylist_ReturnsNotFound()
        {
            var songId = await SeedSongAsync("Everlong", "Foo Fighters");

            var response = await Client.PostAsJsonAsync("/api/playlists/99999/songs", new[] { songId });
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(body, Does.Contain("Playlist not found").IgnoreCase);
            });
        }

        [Test]
        public async Task OpenApiSpec_IsServedWithApiTitle()
        {
            var response = await Client.GetAsync("/openapi/v1.json");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Does.Contain("luftborn"));
            });
        }
    }
}
