using CORE.DTOs;
using System.Net;
using System.Net.Http.Json;

namespace Tests.Integration.Api
{
    public class SongsEndpointsTests : IntegrationTestBase
    {
        [Test]
        public async Task GetAllSongs_WithSeededSongs_ReturnsAllSongs()
        {
            var id1 = await SeedSongAsync("Everlong", "Foo Fighters");
            var id2 = await SeedSongAsync("Creep", "Radiohead");

            var response = await Client.GetAsync("/api/songs");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var songs = await ReadJsonAsync<List<SongDto>>(response)!;

            Assert.Multiple(() =>
            {
                Assert.That(songs, Is.Not.Null);
                Assert.That(songs!, Has.Count.EqualTo(2));
                Assert.That(songs!.Select(s => s.Id), Is.EquivalentTo(new[] { id1, id2 }));
                Assert.That(songs!.Single(s => s.Id == id1).Title, Is.EqualTo("Everlong"));
                Assert.That(songs!.Single(s => s.Id == id1).Artist, Is.EqualTo("Foo Fighters"));
            });
        }

        [Test]
        public async Task GetAllSongs_WithNoSongs_ReturnsEmptyList()
        {
            var response = await Client.GetAsync("/api/songs");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var songs = await ReadJsonAsync<List<SongDto>>(response)!;
            Assert.That(songs, Is.Empty);
        }

        [Test]
        public async Task GetSong_ExistingId_ReturnsSong()
        {
            var songId = await SeedSongAsync("Bohemian Rhapsody", "Queen");

            var response = await Client.GetAsync($"/api/songs/{songId}");

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var song = await ReadJsonAsync<SongDto>(response)!;

            Assert.Multiple(() =>
            {
                Assert.That(song, Is.Not.Null);
                Assert.That(song!.Id, Is.EqualTo(songId));
                Assert.That(song.Title, Is.EqualTo("Bohemian Rhapsody"));
                Assert.That(song.Artist, Is.EqualTo("Queen"));
            });
        }

        [Test]
        public async Task GetSong_UnknownId_ReturnsNotFound()
        {
            var response = await Client.GetAsync("/api/songs/99999");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(body, Does.Contain("not found").IgnoreCase);
            });
        }
    }
}
