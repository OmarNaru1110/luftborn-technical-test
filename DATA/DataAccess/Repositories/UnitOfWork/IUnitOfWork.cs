using DATA.DataAccess.Repositories.IRepositories;
using DATA.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DATA.DataAccess.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IBaseRepository<Playlist> Playlists { get; }
        IBaseRepository<Song> Songs { get; }

        Task<int> CommitAsync();
    }
}
