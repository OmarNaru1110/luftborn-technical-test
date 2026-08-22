using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CORE.DTOs.Playlist
{
    public class CreatePlaylistDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        public IEnumerable<int> SongIds { get; set; } = Enumerable.Empty<int>();
    }
}
