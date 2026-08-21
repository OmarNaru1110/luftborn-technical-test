using System;
using System.Collections.Generic;
using System.Text;

namespace DATA.Models
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Song>? Songs { get; set; }
    }
}
