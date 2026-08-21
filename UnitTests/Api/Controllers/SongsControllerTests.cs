using API.Controllers;
using CORE.DTOs;
using CORE.Enums;
using CORE.Services.IServices;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests.Api.Controllers
{
    public class SongsControllerTests
    {
        private readonly Mock<ISongService> _songService = new();

        private SongsController CreateSut() => new(_songService.Object);

        [Test]
        public async Task GetSong_Found_ReturnsOkWithData()
        {
            var song = new SongDto { Id = 3, Title = "Clocks", Artist = "Coldplay" };
            _songService
                .Setup(s => s.GetSongAsync(3))
                .ReturnsAsync(new ResponseDto<SongDto> { Status = ResultStatus.Success, Data = song });

            var response = await CreateSut().GetSongAsync(3);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<OkObjectResult>());
                Assert.That(((OkObjectResult)response).Value, Is.EqualTo(song));
            });
        }

        [Test]
        public async Task GetSong_NotFound_ReturnsNotFoundWithMessage()
        {
            _songService
                .Setup(s => s.GetSongAsync(404))
                .ReturnsAsync(new ResponseDto<SongDto> { Status = ResultStatus.NotFound, Message = "Song not found." });

            var response = await CreateSut().GetSongAsync(404);

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<NotFoundObjectResult>());
                Assert.That(((NotFoundObjectResult)response).Value, Is.EqualTo("Song not found."));
            });
        }

        [Test]
        public async Task GetAllSongs_AlwaysReturnsOkWithServiceData()
        {
            var songs = new[]
            {
                new SongDto { Id = 1, Title = "Everlong", Artist = "Foo Fighters" },
                new SongDto { Id = 2, Title = "Clocks", Artist = "Coldplay" }
            };
            _songService
                .Setup(s => s.GetAllSongsAsync())
                .ReturnsAsync(new ResponseDto<IEnumerable<SongDto>> { Status = ResultStatus.Success, Data = songs });

            var response = await CreateSut().GetAllSongsAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<OkObjectResult>());
                var value = (IEnumerable<SongDto>)((OkObjectResult)response).Value!;
                Assert.That(value.Select(s => s.Id), Is.EqualTo(new[] { 1, 2 }));
            });
        }

        [Test]
        public async Task GetAllSongs_EmptyCatalog_ReturnsOkWithEmptyCollection()
        {
            _songService
                .Setup(s => s.GetAllSongsAsync())
                .ReturnsAsync(new ResponseDto<IEnumerable<SongDto>> { Status = ResultStatus.Success, Data = [] });

            var response = await CreateSut().GetAllSongsAsync();

            Assert.Multiple(() =>
            {
                Assert.That(response, Is.TypeOf<OkObjectResult>());
                Assert.That(((IEnumerable<SongDto>)((OkObjectResult)response).Value!), Is.Empty);
            });
        }
    }
}
