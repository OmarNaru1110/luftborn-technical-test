using CORE.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CORE.Services.IServices
{
    public interface ISongService
    {
        Task<SongDto?> GetSongAsync(int songId);
    }
}
