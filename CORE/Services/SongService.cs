using CORE.DTOs;
using CORE.Services.IServices;
using DATA.DataAccess.Repositories.UnitOfWork;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace CORE.Services
{
    public class SongService : ISongService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SongService> _logger;

        public SongService(IUnitOfWork unitOfWork, ILogger<SongService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<SongDto?> GetSongAsync(int songId)
        {
            _logger.LogInformation($"Fetching song with ID: {songId}");
            
            var song = await _unitOfWork.Songs.GetAsync(songId);
            if(song == null)
            {
                _logger.LogWarning($"Song with ID: {songId} not found.");
                return null;
            }
            return new SongDto
            {
                Id = song.Id,
                Title = song.Title,
                Artist = song.Artist
            };
        }
    }
}
