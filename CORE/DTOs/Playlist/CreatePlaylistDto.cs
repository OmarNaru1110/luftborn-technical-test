using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CORE.DTOs.Playlist
{
    public class CreatePlaylistDto
    {
        [Required]
        public string Name { get; set; }
        public IEnumerable<int> SongIds { get; set; } = Enumerable.Empty<int>();
    }
}
