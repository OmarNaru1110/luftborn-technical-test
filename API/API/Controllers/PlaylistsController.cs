using API.Services;
using CORE.DTOs.Playlist;
using CORE.Enums;
using CORE.Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlaylistsController : ControllerBase
    {
        private readonly IPlaylistService _playlistService;
        private readonly ICurrentUser _currentUser;

        public PlaylistsController(IPlaylistService playlistService, ICurrentUser currentUser)
        {
            _playlistService = playlistService;
            _currentUser = currentUser;
        }

        [HttpPost]
        public async Task<IActionResult> CreatePlaylistAsync(CreatePlaylistDto dto)
        {
            var result = await _playlistService.CreatePlaylistAsync(dto, _currentUser.Id);
            if(result.Status == ResultStatus.Invalid)
            {
                return BadRequest(result.Message);
            }
            return Created((string?)null, result.Data);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlaylistAsync(int id)
        {
            var result = await _playlistService.GetPlaylistAsync(id);
            if(result.Status == ResultStatus.NotFound)
            {
                return NotFound(result.Message);
            }
            return Ok(result.Data);
        }
        [HttpPost("{id}/songs")]
        public async Task<IActionResult> AddSongsToPlaylist(int id, List<int>? songIds)
        {
            var result = await _playlistService.AddSongsToPlaylistAsync(id, songIds);
            if (result.Status == ResultStatus.Invalid)
            {
                return BadRequest(result.Message);
            } 
            else if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result.Message);
            }
            return Ok(result.Data);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePlaylistAsync(int id)
        {
            var result = await _playlistService.DeletePlaylistAsync(id);
            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result.Message);
            }
            return NoContent();
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePlaylistAsync(int id, UpdatePlaylistDto dto)
        {
            var result = await _playlistService.UpdatePlaylistAsync(id, dto);
            if (result.Status == ResultStatus.NotFound)
            {
                return NotFound(result.Message);
            }
            return Ok(result.Data);
        }
        [HttpGet]
        public async Task<IActionResult> GetPlaylistsAsync()
        {
            int userId = _currentUser.Id;
            var result = await _playlistService.GetPlaylistsAsync(userId);
            if (result.Status == ResultStatus.Unauthorized)
            {
                return Unauthorized(result.Message);
            }
            return Ok(result.Data);
        }
    }
}
