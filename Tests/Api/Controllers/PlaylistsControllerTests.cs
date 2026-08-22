using API.Controllers;
using API.Services;
using CORE.DTOs;
using CORE.DTOs.Playlist;
using CORE.Enums;
using CORE.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tests.Api.Controllers
{
    public class PlaylistsControllerTests
    {
        private readonly Mock<IPlaylistService> _playlistService = new();
        private readonly Mock<ICurrentUser> _currentUser = new();

        public PlaylistsControllerTests()
        {
            _currentUser.SetupGet(u => u.Id).Returns(1);
        }

        private PlaylistsController CreateSut() =>
            new(_playlistService.Object, _currentUser.Object);

        #region CreatePlaylist

        [Test]
        public async Task CreatePlaylist_Success_ReturnsCreatedWithPlaylistData()
        {
            var dto = new PlaylistDto { Id = 1, Name = "Mix", UserId = 1 };
            _playlistService
                .Setup(s => s.CreatePlaylistAsync(It.IsAny<CreatePlaylistDto>(), It.IsAny<int?>()))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Success, Data = dto });

            var response = await CreateSut().CreatePlaylistAsync(new CreatePlaylistDto { Name = "Mix" });

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<CreatedResult>());
                Assert.That(((CreatedResult)response).Value, Is.EqualTo(dto));
            });
        }

        [Test]
        public async Task CreatePlaylist_ServiceFailure_ReturnsBadRequestWithMessage()
        {
            _playlistService
                .Setup(s => s.CreatePlaylistAsync(It.IsAny<CreatePlaylistDto>(), It.IsAny<int?>()))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Invalid, Message = "user Id is null" });

            var response = await CreateSut().CreatePlaylistAsync(new CreatePlaylistDto());

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<BadRequestObjectResult>());
                Assert.That(((BadRequestObjectResult)response).Value, Is.EqualTo("user Id is null"));
            });
        }

        [Test]
        public async Task CreatePlaylist_PassesCurrentUserAndDtoToService()
        {
            var dto = new CreatePlaylistDto { Name = "Road Trip" };
            _playlistService
                .Setup(s => s.CreatePlaylistAsync(dto, 1))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Success, Data = new PlaylistDto() });

            await CreateSut().CreatePlaylistAsync(dto);

            _playlistService.Verify(s => s.CreatePlaylistAsync(dto, 1), Times.Once);
        }

        #endregion

        #region GetPlaylist

        [Test]
        public async Task GetPlaylist_Found_ReturnsOkWithData()
        {
            var playlist = new PlaylistDto { Id = 4, Name = "Chill" };
            _playlistService
                .Setup(s => s.GetPlaylistAsync(4))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Success, Data = playlist });

            var response = await CreateSut().GetPlaylistAsync(4);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<OkObjectResult>());
                Assert.That(((OkObjectResult)response).Value, Is.EqualTo(playlist));
            });
        }

        [Test]
        public async Task GetPlaylist_NotFound_ReturnsNotFoundWithMessage()
        {
            _playlistService
                .Setup(s => s.GetPlaylistAsync(404))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.NotFound, Message = "Playlist not found" });

            var response = await CreateSut().GetPlaylistAsync(404);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
                Assert.That(((NotFoundObjectResult)response).Value, Is.EqualTo("Playlist not found"));
            });
        }

        #endregion

        #region AddSongsToPlaylist

        [Test]
        public async Task AddSongsToPlaylist_Success_ReturnsOkWithData()
        {
            var data = new PlaylistDto { Id = 2, Name = "Gym", Songs = new[] { new SongDto { Id = 9 } } };
            _playlistService
                .Setup(s => s.AddSongsToPlaylistAsync(2, It.IsAny<List<int>>()))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Success, Data = data });

            var response = await CreateSut().AddSongsToPlaylist(2, new List<int> { 9 });

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<OkObjectResult>());
                Assert.That(((OkObjectResult)response).Value, Is.EqualTo(data));
            });
        }

        [Test]
        public async Task AddSongsToPlaylist_Failure_ReturnsBadRequestWithMessage()
        {
            _playlistService
                .Setup(s => s.AddSongsToPlaylistAsync(2, It.IsAny<List<int>>()))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Invalid, Message = "No songs provided" });

            var response = await CreateSut().AddSongsToPlaylist(2, new List<int>());

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<BadRequestObjectResult>());
                Assert.That(((BadRequestObjectResult)response).Value, Is.EqualTo("No songs provided"));
            });
        }

        [Test]
        public async Task AddSongsToPlaylist_ForwardSongIdListToService()
        {
            var songIds = new List<int> { 5, 6 };
            _playlistService
                .Setup(s => s.AddSongsToPlaylistAsync(3, songIds))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Success, Data = new PlaylistDto() });

            await CreateSut().AddSongsToPlaylist(3, songIds);

            _playlistService.Verify(s => s.AddSongsToPlaylistAsync(3, songIds), Times.Once);
        }

        #endregion

        #region DeletePlaylist

        [Test]
        public async Task DeletePlaylist_Success_ReturnsNoContent()
        {
            _playlistService
                .Setup(s => s.DeletePlaylistAsync(7))
                .ReturnsAsync(new ResponseDto<object> { Status = ResultStatus.Success });

            var response = await CreateSut().DeletePlaylistAsync(7);

            Assert.That(response, Is.TypeOf<NoContentResult>());
        }

        [Test]
        public async Task DeletePlaylist_NotFound_ReturnsNotFoundWithMessage()
        {
            _playlistService
                .Setup(s => s.DeletePlaylistAsync(7))
                .ReturnsAsync(new ResponseDto<object> { Status = ResultStatus.NotFound, Message = "Playlist not found" });

            var response = await CreateSut().DeletePlaylistAsync(7);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
                Assert.That(((NotFoundObjectResult)response).Value, Is.EqualTo("Playlist not found"));
            });
        }

        #endregion

        #region UpdatePlaylist

        [Test]
        public async Task UpdatePlaylist_Success_ReturnsOkWithData()
        {
            var updated = new PlaylistDto { Id = 8, Name = "Renamed" };
            _playlistService
                .Setup(s => s.UpdatePlaylistAsync(8, It.IsAny<UpdatePlaylistDto>()))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.Success, Data = updated });

            var response = await CreateSut().UpdatePlaylistAsync(8, new UpdatePlaylistDto { Name = "Renamed" });

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<OkObjectResult>());
                Assert.That(((OkObjectResult)response).Value, Is.EqualTo(updated));
            });
        }

        [Test]
        public async Task UpdatePlaylist_Failure_ReturnsNotFoundWithMessage()
        {
            _playlistService
                .Setup(s => s.UpdatePlaylistAsync(8, It.IsAny<UpdatePlaylistDto>()))
                .ReturnsAsync(new ResponseDto<PlaylistDto> { Status = ResultStatus.NotFound, Message = "Playlist not found" });

            var response = await CreateSut().UpdatePlaylistAsync(8, new UpdatePlaylistDto());

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
                Assert.That(((NotFoundObjectResult)response).Value, Is.EqualTo("Playlist not found"));
            });
        }

        #endregion
    }
}
