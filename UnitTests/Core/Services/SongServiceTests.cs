using CORE.Services;
using DATA.DataAccess.Repositories.IRepositories;
using DATA.DataAccess.Repositories.UnitOfWork;
using DATA.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests.Core.Services
{
    public class SongServiceTests
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Song>> _songs = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _songs = new Mock<IBaseRepository<Song>>();
            _unitOfWork.SetupGet(u => u.Songs).Returns(_songs.Object);
        }

        private SongService CreateSut() =>
            new(_unitOfWork.Object, Mock.Of<ILogger<SongService>>());

        private static Song BuildSong(int id, string title, string artist) =>
            new() { Id = id, Title = title, Artist = artist };

        [Test]
        public async Task GetAllSongsAsync_ReturnsAllSongsMappedToDtos()
        {
            _songs
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(new[]
                {
                    BuildSong(1, "Everlong", "Foo Fighters"),
                    BuildSong(2, "Clocks", "Coldplay")
                });

            var result = await CreateSut().GetAllSongsAsync();

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Message, Is.Null);
                var songs = result.Data!.ToList();
                Assert.That(songs, Has.Count.EqualTo(2));
                Assert.That(songs[0].Id, Is.EqualTo(1));
                Assert.That(songs[0].Title, Is.EqualTo("Everlong"));
                Assert.That(songs[0].Artist, Is.EqualTo("Foo Fighters"));
                Assert.That(songs[1].Id, Is.EqualTo(2));
                Assert.That(songs[1].Title, Is.EqualTo("Clocks"));
                Assert.That(songs[1].Artist, Is.EqualTo("Coldplay"));
            });
        }

        [Test]
        public async Task GetAllSongsAsync_NoSongs_ReturnsEmptyCollection()
        {
            _songs.Setup(r => r.GetAllAsync()).ReturnsAsync(Array.Empty<Song>());

            var result = await CreateSut().GetAllSongsAsync();

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Data, Is.Empty);
            });
        }

        [Test]
        public async Task GetSongAsync_Found_ReturnsMappedDto()
        {
            _songs
                .Setup(r => r.GetAsync(5))
                .ReturnsAsync(BuildSong(5, "Bohemian Rhapsody", "Queen"));

            var result = await CreateSut().GetSongAsync(5);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.True);
                Assert.That(result.Message, Is.Null);
                Assert.That(result.Data!.Id, Is.EqualTo(5));
                Assert.That(result.Data!.Title, Is.EqualTo("Bohemian Rhapsody"));
                Assert.That(result.Data!.Artist, Is.EqualTo("Queen"));
            });
        }

        [Test]
        public async Task GetSongAsync_NotFound_ReturnsFailureWithMessage()
        {
            _songs.Setup(r => r.GetAsync(404)).ReturnsAsync((Song?)null);

            var result = await CreateSut().GetSongAsync(404);

            Assert.Multiple(() =>
            {
                Assert.That(result.IsSuccess, Is.False);
                Assert.That(result.Message, Is.EqualTo("Song not found."));
                Assert.That(result.Data, Is.Null);
            });
        }
    }
}
