using CORE.Enums;
using CORE.Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SongsController : ControllerBase
    {
        private readonly ISongService _songService;

        public SongsController(ISongService songService)
        {
            _songService = songService;
        }
        [HttpGet("{songId}")]
        public async Task<IActionResult> GetSongAsync(int songId)
        {
            var result = await _songService.GetSongAsync(songId);
            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result.Message);
            }
            return Ok(result.Data);
        }
        [HttpGet]
        public async Task<IActionResult> GetAllSongsAsync()
        {
            var result = await _songService.GetAllSongsAsync();
            return Ok(result.Data);
        }
    }
}
