using System.ComponentModel.DataAnnotations;

namespace CORE.DTOs.Playlist
{
    public class UpdatePlaylistDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }
}
