using CORE.DTOs;
using CORE.DTOs.Playlist;
using System;
using System.Collections.Generic;
using System.Text;

namespace CORE.Services.IServices
{
    public interface IPlaylistService
    {
        Task<ResponseDto<PlaylistDto>> CreatePlaylistAsync(CreatePlaylistDto dto, int? userId);
        Task<ResponseDto<PlaylistDto>> GetPlaylistAsync(int id);
        Task<ResponseDto<PlaylistDto>> AddSongsToPlaylistAsync(int playlistId, List<int>? songIds);
    }
}
