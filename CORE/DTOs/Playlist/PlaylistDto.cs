using DATA.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace CORE.DTOs.Playlist
{
    public class PlaylistDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public IEnumerable<SongDto> Songs { get; set; } = Enumerable.Empty<SongDto>();
    }
}
