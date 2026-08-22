using CORE.DTOs.Playlist;
using CORE.Enums;
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

namespace Tests.Core.Services
{
    public class PlaylistServiceTests
    {
        private Mock<IUnitOfWork> _unitOfWork = null!;
        private Mock<IBaseRepository<Playlist>> _playlists = null!;
        private Mock<IBaseRepository<Song>> _songs = null!;

        [SetUp]
        public void SetUp()
        {
            _unitOfWork = new Mock<IUnitOfWork>();
            _playlists = new Mock<IBaseRepository<Playlist>>();
            _songs = new Mock<IBaseRepository<Song>>();
            _unitOfWork.SetupGet(u => u.Playlists).Returns(_playlists.Object);
            _unitOfWork.SetupGet(u => u.Songs).Returns(_songs.Object);
        }

        private PlaylistService CreateSut() =>
            new(_unitOfWork.Object, Mock.Of<ILogger<PlaylistService>>());

        private static Playlist BuildPlaylist(
            int id,
            string name = "My Playlist",
            int userId = 1,
            IEnumerable<Song>? songs = null) =>
            new()
            {
                Id = id,
                Name = name,
                UserId = userId,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Songs = songs is null ? new List<Song>() : new List<Song>(songs)
            };

        private static Song BuildSong(int id, string title = "Title", string artist = "Artist") =>
            new() { Id = id, Title = title, Artist = artist };

        #region AddSongsToPlaylistAsync

        [Test]
        public async Task AddSongsToPlaylistAsync_NullSongIds_ReturnsFailure()
        {
            var result = await CreateSut().AddSongsToPlaylistAsync(1, null);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
                Assert.That(result.Message, Is.EqualTo("No songs provided"));
                Assert.That(result.Data, Is.Null);
            });
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_EmptySongIds_ReturnsFailure()
        {
            var result = await CreateSut().AddSongsToPlaylistAsync(1, new List<int>());

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
                Assert.That(result.Message, Is.EqualTo("No songs provided"));
            });
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_NoSongsProvided_DoesNotTouchRepositories()
        {
            await CreateSut().AddSongsToPlaylistAsync(1, null);

            _playlists.Verify(r => r.GetAsync(It.IsAny<int>(), It.IsAny<string[]>()), Times.Never);
            _songs.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_PlaylistNotFound_ReturnsFailure()
        {
            _playlists
                .Setup(r => r.GetAsync(99, It.IsAny<string[]>()))
                .ReturnsAsync((Playlist?)null);

            var result = await CreateSut().AddSongsToPlaylistAsync(99, new List<int> { 1 });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
                Assert.That(result.Message, Is.EqualTo("Playlist not found"));
            });
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_AllSongsAlreadyPresent_ReturnsWithoutCommitting()
        {
            var playlist = BuildPlaylist(1, songs: new[] { BuildSong(1), BuildSong(2) });
            _playlists.Setup(r => r.GetAsync(1, It.IsAny<string[]>())).ReturnsAsync(playlist);

            var result = await CreateSut().AddSongsToPlaylistAsync(1, new List<int> { 1, 2, 2 });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data!.Id, Is.EqualTo(1));
                Assert.That(result.Data!.Songs.Select(s => s.Id), Is.EqualTo(new[] { 1, 2 }));
            });
            _songs.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_MixedRequest_AddsOnlyNewUniqueSongsAndCommits()
        {
            var playlist = BuildPlaylist(1, songs: new[] { BuildSong(1, "One", "A") });
            _playlists.Setup(r => r.GetAsync(1, It.IsAny<string[]>())).ReturnsAsync(playlist);
            _songs
                .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 2, 3 }))))
                .ReturnsAsync(new[] { BuildSong(2, "Two", "B"), BuildSong(3, "Three", "C") });

            var result = await CreateSut().AddSongsToPlaylistAsync(1, new List<int> { 2, 3, 1, 2 });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data!.Songs.Select(s => s.Id), Is.EqualTo(new[] { 1, 2, 3 }));
                Assert.That(playlist.Songs!.Select(s => s.Id), Is.EqualTo(new[] { 1, 2, 3 }));
            });
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_RequestedSongsMissingFromCatalog_CommitsNothingNew()
        {
            var playlist = BuildPlaylist(1, songs: new[] { BuildSong(1) });
            _playlists.Setup(r => r.GetAsync(1, It.IsAny<string[]>())).ReturnsAsync(playlist);
            _songs
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(Array.Empty<Song>());

            var result = await CreateSut().AddSongsToPlaylistAsync(1, new List<int> { 404 });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data!.Songs.Select(s => s.Id), Is.EqualTo(new[] { 1 }));
                Assert.That(playlist.Songs!, Has.Count.EqualTo(1));
            });
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_PlaylistSongsCollectionIsNull_InitializesAndAdds()
        {
            var playlist = BuildPlaylist(1);
            playlist.Songs = null;
            _playlists.Setup(r => r.GetAsync(1, It.IsAny<string[]>())).ReturnsAsync(playlist);
            _songs
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new[] { BuildSong(7) });

            var result = await CreateSut().AddSongsToPlaylistAsync(1, new List<int> { 7 });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(playlist.Songs, Is.Not.Null);
                Assert.That(result.Data!.Songs.Select(s => s.Id), Is.EqualTo(new[] { 7 }));
            });
        }

        [Test]
        public async Task AddSongsToPlaylistAsync_Success_MapsSongFieldsCorrectly()
        {
            var playlist = BuildPlaylist(1);
            _playlists.Setup(r => r.GetAsync(1, It.IsAny<string[]>())).ReturnsAsync(playlist);
            _songs
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new[] { BuildSong(5, "Everlong", "Foo Fighters") });

            var result = await CreateSut().AddSongsToPlaylistAsync(1, new List<int> { 5 });

            var song = result.Data!.Songs.Single();
            Assert.Multiple(() =>
            {
                Assert.That(song.Id, Is.EqualTo(5));
                Assert.That(song.Title, Is.EqualTo("Everlong"));
                Assert.That(song.Artist, Is.EqualTo("Foo Fighters"));
            });
        }

        #endregion

        #region CreatePlaylistAsync

        [Test]
        public async Task CreatePlaylistAsync_NullUserId_ReturnsFailure()
        {
            var result = await CreateSut().CreatePlaylistAsync(new CreatePlaylistDto { Name = "X" }, null);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
                Assert.That(result.Message, Is.EqualTo("user Id is null"));
            });
            _playlists.Verify(r => r.AddOrUpdateAsync(It.IsAny<Playlist>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task CreatePlaylistAsync_ValidRequest_PersistsPlaylistWithSongsAndUser()
        {
            Playlist? added = null;
            _playlists
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Playlist>()))
                .Callback<Playlist>(p =>
                {
                    p.Id = 42;
                    added = p;
                })
                .ReturnsAsync((Playlist p) => p);
            _songs
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(new[] { BuildSong(10), BuildSong(11) });
            _playlists
                .Setup(r => r.GetAsync(42, It.IsAny<string[]>()))
                .ReturnsAsync(() => added!);

            var dto = new CreatePlaylistDto { Name = "Road Trip", SongIds = new[] { 10, 11 } };
            var result = await CreateSut().CreatePlaylistAsync(dto, userId: 9);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data, Is.Not.Null);
                Assert.That(result.Data!.Id, Is.EqualTo(42));
                Assert.That(result.Data!.Name, Is.EqualTo("Road Trip"));
                Assert.That(result.Data!.UserId, Is.EqualTo(9));
                Assert.That(result.Data!.Songs.Select(s => s.Id), Is.EqualTo(new[] { 10, 11 }));
            });
            _playlists.Verify(r => r.AddOrUpdateAsync(It.IsAny<Playlist>()), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task CreatePlaylistAsync_NoSongIds_CreatesEmptyPlaylist()
        {
            Playlist? added = null;
            _playlists
                .Setup(r => r.AddOrUpdateAsync(It.IsAny<Playlist>()))
                .Callback<Playlist>(p =>
                {
                    p.Id = 7;
                    added = p;
                })
                .ReturnsAsync((Playlist p) => p);
            _songs
                .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(Array.Empty<Song>());
            _playlists
                .Setup(r => r.GetAsync(7, It.IsAny<string[]>()))
                .ReturnsAsync(() => added!);

            var result = await CreateSut().CreatePlaylistAsync(new CreatePlaylistDto { Name = "Empty" }, userId: 3);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data!.Songs, Is.Empty);
            });
        }

        #endregion

        #region DeletePlaylistAsync

        [Test]
        public async Task DeletePlaylistAsync_NotFound_ReturnsFailure()
        {
            _playlists.Setup(r => r.GetAsync(123)).ReturnsAsync((Playlist?)null);

            var result = await CreateSut().DeletePlaylistAsync(123);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
                Assert.That(result.Message, Is.EqualTo("Playlist not found"));
            });
            _playlists.Verify(r => r.Delete(It.IsAny<Playlist>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task DeletePlaylistAsync_Found_DeletesAndCommits()
        {
            var playlist = BuildPlaylist(55);
            _playlists.Setup(r => r.GetAsync(55)).ReturnsAsync(playlist);

            var result = await CreateSut().DeletePlaylistAsync(55);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data, Is.Null);
            });
            _playlists.Verify(r => r.Delete(playlist), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        #endregion

        #region GetPlaylistAsync

        [Test]
        public async Task GetPlaylistAsync_NotFound_ReturnsFailure()
        {
            _playlists.Setup(r => r.GetAsync(-1, It.IsAny<string[]>())).ReturnsAsync((Playlist?)null);

            var result = await CreateSut().GetPlaylistAsync(-1);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
                Assert.That(result.Message, Is.EqualTo("Playlist not found"));
                Assert.That(result.Data, Is.Null);
            });
        }

        [Test]
        public async Task GetPlaylistAsync_Found_MapsAllFieldsIncludingUserAndTimestamp()
        {
            var created = new DateTime(2026, 8, 21, 12, 0, 0, DateTimeKind.Utc);
            var playlist = new Playlist
            {
                Id = 8,
                Name = "Chill",
                UserId = 21,
                CreatedAt = created,
                Songs = new List<Song> { BuildSong(1) }
            };
            _playlists.Setup(r => r.GetAsync(8, It.IsAny<string[]>())).ReturnsAsync(playlist);

            var result = await CreateSut().GetPlaylistAsync(8);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data!.Id, Is.EqualTo(8));
                Assert.That(result.Data!.Name, Is.EqualTo("Chill"));
                Assert.That(result.Data!.UserId, Is.EqualTo(21));
                Assert.That(result.Data!.CreatedAt, Is.EqualTo(created));
                Assert.That(result.Data!.Songs.Select(s => s.Id), Is.EqualTo(new[] { 1 }));
            });
        }

        [Test]
        public async Task GetPlaylistAsync_SongsCollectionNull_ReturnsEmptySongList()
        {
            var playlist = BuildPlaylist(3);
            playlist.Songs = null;
            _playlists.Setup(r => r.GetAsync(3, It.IsAny<string[]>())).ReturnsAsync(playlist);

            var result = await CreateSut().GetPlaylistAsync(3);

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(result.Data!.Songs, Is.Empty);
            });
        }

        #endregion

        #region UpdatePlaylistAsync

        [Test]
        public async Task UpdatePlaylistAsync_NotFound_ReturnsFailure()
        {
            _playlists.Setup(r => r.GetAsync(404)).ReturnsAsync((Playlist?)null);

            var result = await CreateSut().UpdatePlaylistAsync(404, new UpdatePlaylistDto { Name = "New" });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
                Assert.That(result.Message, Is.EqualTo("Playlist not found"));
            });
            _playlists.Verify(r => r.AddOrUpdateAsync(It.IsAny<Playlist>()), Times.Never);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Never);
        }

        [Test]
        public async Task UpdatePlaylistAsync_NewName_RenamesPersistsAndReturnsUpdatedData()
        {
            var playlist = BuildPlaylist(12, name: "Old Name");
            SetupUpdateRoundTrip(playlist);

            var result = await CreateSut().UpdatePlaylistAsync(12, new UpdatePlaylistDto { Name = "Brand New" });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(playlist.Name, Is.EqualTo("Brand New"));
                Assert.That(result.Data!.Name, Is.EqualTo("Brand New"));
                Assert.That(result.Data!.Id, Is.EqualTo(12));
            });
            _playlists.Verify(r => r.AddOrUpdateAsync(playlist), Times.Once);
            _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        }

        [Test]
        public async Task UpdatePlaylistAsync_ExplicitNullName_KeepsExistingName()
        {
            var playlist = BuildPlaylist(13, name: "Keep Me");
            SetupUpdateRoundTrip(playlist);

            var result = await CreateSut().UpdatePlaylistAsync(13, new UpdatePlaylistDto { Name = null! });

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(playlist.Name, Is.EqualTo("Keep Me"));
                Assert.That(result.Data!.Name, Is.EqualTo("Keep Me"));
            });
        }

        [Test]
        public async Task UpdatePlaylistAsync_EmptyStringName_OverwritesExistingName()
        {
            var playlist = BuildPlaylist(14, name: "Original");
            SetupUpdateRoundTrip(playlist);

            var result = await CreateSut().UpdatePlaylistAsync(14, new UpdatePlaylistDto());

            Assert.Multiple(() =>
            {
                Assert.That(result.Status, Is.EqualTo(ResultStatus.Success));
                Assert.That(playlist.Name, Is.Empty);
                Assert.That(result.Data!.Name, Is.Empty);
            });
        }

        private void SetupUpdateRoundTrip(Playlist playlist)
        {
            _playlists.Setup(r => r.GetAsync(playlist.Id)).ReturnsAsync(playlist);
            _playlists
                .Setup(r => r.AddOrUpdateAsync(playlist))
                .ReturnsAsync(playlist);
            _playlists
                .Setup(r => r.GetAsync(playlist.Id, It.IsAny<string[]>()))
                .ReturnsAsync(playlist);
        }

        #endregion
    }
}
