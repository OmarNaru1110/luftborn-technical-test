using CORE.DTOs;
using CORE.DTOs.Playlist;
using CORE.Services.IServices;
using DATA.DataAccess.Repositories.UnitOfWork;
using DATA.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CORE.Services
{
    public class PlaylistService : IPlaylistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PlaylistService> _logger;

        public PlaylistService(IUnitOfWork unitOfWork, ILogger<PlaylistService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<ResponseDto<PlaylistDto>> AddSongsToPlaylistAsync(int playlistId, List<int>? songIds)
        {
            _logger.LogInformation("Adding songs to playlist {PlaylistId}", playlistId);
            if (songIds.IsNullOrEmpty())
            {
                _logger.LogWarning("No songs provided for playlist {PlaylistId}", playlistId);
                return new ResponseDto<PlaylistDto>
                {
                    IsSuccess = false,
                    Message = "No songs provided"
                };
            }

            var playlist = await _unitOfWork.Playlists.GetAsync(playlistId, new string[] { nameof(Playlist.Songs) });

            if (playlist == null)
            {
                _logger.LogWarning($"Playlist with Id {playlistId} not found.");
                return new ResponseDto<PlaylistDto>
                {
                    IsSuccess = false,
                    Message = "Playlist not found"
                };
            }

            var playlistSongIds = playlist.Songs?.Select(s => s.Id).ToList() ?? [];

            var newSongIds = songIds
                .Except(playlistSongIds)
                .ToList();

            if (newSongIds.Count == 0)
            {
                _logger.LogInformation(
                    "All requested songs are already in playlist {PlaylistId}",
                    playlistId);

                return new ResponseDto<PlaylistDto>
                {
                    IsSuccess = true,
                    Data = new PlaylistDto
                    {
                        Id = playlist.Id,
                        Name = playlist.Name,
                        Songs = playlist.Songs?.Select(s => new SongDto
                        {
                            Id = s.Id,
                            Title = s.Title,
                            Artist = s.Artist
                        }).ToList() ?? []
                    }
                };
            }

            var songs = await _unitOfWork.Songs.GetByIdsAsync(newSongIds);
            playlist.Songs ??= [];
            foreach (var song in songs)
            {
                playlist.Songs.Add(song);
            }

            await _unitOfWork.CommitAsync();

            _logger.LogInformation(
                "Successfully added {Count} songs to playlist {PlaylistId}",
                songs.Count(),
                playlistId);
            return new ResponseDto<PlaylistDto>
            {
                IsSuccess = true,
                Data = new PlaylistDto
                {
                    Id = playlist.Id,
                    Name = playlist.Name,
                    Songs = playlist.Songs.Select(s => new SongDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Artist = s.Artist
                    }).ToList()
                }
            };
        }

        public async Task<ResponseDto<PlaylistDto>> CreatePlaylistAsync(CreatePlaylistDto dto, int? userId)
        {
            _logger.LogInformation("Creating a new playlist for user {UserId}", userId);

            if (userId == null)
            {
                _logger.LogError("User Id is null. Cannot create playlist.");
                return new ResponseDto<PlaylistDto>
                {
                    IsSuccess = false,
                    Message = "user Id is null"
                };
            }

            var songs = await _unitOfWork.Songs.GetByIdsAsync(dto.SongIds);
            var playlist = new Playlist
            {
                Name = dto.Name,
                UserId = userId.Value,
                Songs = [.. songs]
            };

            await _unitOfWork.Playlists.AddOrUpdateAsync(playlist);
            await _unitOfWork.CommitAsync();

            var result = await GetPlaylistAsync(playlist.Id);

            return new ResponseDto<PlaylistDto>
            {
                IsSuccess = true,
                Data = result.Data
            };
        }

        public async Task<ResponseDto<PlaylistDto>> GetPlaylistAsync(int id)
        {
            _logger.LogInformation($"Fetching playlist with Id {id}");
            
            var playlist = await _unitOfWork.Playlists.GetAsync(id, new string[] { nameof(Playlist.Songs) });

            if(playlist == null)
            {
                _logger.LogWarning($"Playlist with Id {id} not found.");
                return new ResponseDto<PlaylistDto>
                {
                    IsSuccess = false,
                    Message = "Playlist not found"
                };
            }

            return new ResponseDto<PlaylistDto>
            {
                IsSuccess = true,
                Data = new PlaylistDto
                {
                    Id = playlist.Id,
                    UserId = playlist.UserId,
                    Name = playlist.Name,
                    CreatedAt = playlist.CreatedAt,
                    Songs = playlist.Songs?.Select(s => new SongDto
                    {
                        Id = s.Id,
                        Title = s.Title,
                        Artist = s.Artist
                    }).ToList() ?? []
                }
            };
        }
    }
}
