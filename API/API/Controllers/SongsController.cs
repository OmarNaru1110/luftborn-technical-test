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
            var song = await _songService.GetSongAsync(songId);
            if (song == null)
            {
                return NotFound();
            }
            return Ok(song);
        }
    }
}
