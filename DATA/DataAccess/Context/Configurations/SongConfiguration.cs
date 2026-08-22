using DATA.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DATA.DataAccess.Context.Configurations
{
    public class SongConfiguration : IEntityTypeConfiguration<Song>
    {
        public void Configure(EntityTypeBuilder<Song> builder)
        {
            builder.Property(s => s.Title)
                .HasMaxLength(100);
            
            builder.Property(s => s.Artist)
                .HasMaxLength(100);

            builder.HasMany(s=> s.Playlists)
                .WithMany(p => p.Songs)
                .UsingEntity<PlaylistSong>(
                right => right
                    .HasOne<Playlist>()
                    .WithMany()
                    .HasForeignKey(x => x.PlaylistId)
                    .OnDelete(DeleteBehavior.Cascade),
                left => left
                    .HasOne<Song>()
                    .WithMany()
                    .HasForeignKey(x => x.SongId)
                    .OnDelete(DeleteBehavior.Cascade));

            builder.ToTable("Songs");
        }
    }
}
