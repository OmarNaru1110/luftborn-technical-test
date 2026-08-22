using DATA.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DATA.DataAccess.Context.Configurations
{
    public class PlaylistConfiguration : IEntityTypeConfiguration<Playlist>
    {
        public void Configure(EntityTypeBuilder<Playlist> builder)
        {
            builder.Property(p => p.Name)
                .HasMaxLength(100);

            builder.HasIndex(p => p.UserId);
            
            builder.ToTable("Playlists");
        }
    }
}
