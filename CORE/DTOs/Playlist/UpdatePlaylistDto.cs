using System.ComponentModel.DataAnnotations;

namespace CORE.DTOs.Playlist
{
    public class UpdatePlaylistDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;
    }
}
