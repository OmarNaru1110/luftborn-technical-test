using System;
using System.Collections.Generic;
using System.Text;

namespace DATA.Models
{
    public class Song
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public ICollection<Playlist>? Playlists { get; set; }
    }
}
