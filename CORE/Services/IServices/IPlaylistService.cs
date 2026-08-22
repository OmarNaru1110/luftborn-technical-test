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
        Task<ResponseDto<List<PlaylistDto>>> GetPlaylistsAsync(int? userId);
        Task<ResponseDto<PlaylistDto>> AddSongsToPlaylistAsync(int playlistId, List<int>? songIds);
        Task<ResponseDto<PlaylistDto>> UpdatePlaylistAsync(int playlistId, UpdatePlaylistDto dto);
        Task<ResponseDto<object>> DeletePlaylistAsync(int playlistId);
    }
}
