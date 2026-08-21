using CORE.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CORE.Services.IServices
{
    public interface ISongService
    {
        Task<ResponseDto<SongDto>> GetSongAsync(int songId);
        Task<ResponseDto<IEnumerable<SongDto>>> GetAllSongsAsync();
    }
}
