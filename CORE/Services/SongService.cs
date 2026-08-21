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

        public async Task<ResponseDto<IEnumerable<SongDto>>> GetAllSongsAsync()
        {
            _logger.LogInformation("Fetching all songs");
            var songDtos = (await _unitOfWork.Songs.GetAllAsync())
                .Select(song => new SongDto
                {
                    Id = song.Id,
                    Title = song.Title,
                    Artist = song.Artist
                })
                .ToList();
            return new ResponseDto<IEnumerable<SongDto>> { IsSuccess = true, Data = songDtos };
        }

        public async Task<ResponseDto<SongDto>> GetSongAsync(int songId)
        {
            _logger.LogInformation($"Fetching song with ID: {songId}");
            
            var song = await _unitOfWork.Songs.GetAsync(songId);
            if(song == null)
            {
                _logger.LogWarning($"Song with ID: {songId} not found.");
                return new ResponseDto<SongDto> { IsSuccess = false, Message = "Song not found." };
            }
            return new ResponseDto<SongDto> { 
                IsSuccess = true, 
                Data = new SongDto
                {
                    Id = song.Id,
                    Title = song.Title,
                    Artist = song.Artist
                } 
            };
        }
    }
}
