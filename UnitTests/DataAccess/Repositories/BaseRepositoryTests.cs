using DATA.DataAccess.Context;
using DATA.DataAccess.Repositories;
using DATA.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnitTests.DataAccess.Repositories
{
    public class BaseRepositoryTests
    {
        private static AppDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        private static Song BuildSong(int id, string title = "Title", string artist = "Artist") =>
            new() { Id = id, Title = title, Artist = artist };

        #region GetAsync

        [Test]
        public async Task GetAsync_ExistingId_ReturnsEntity()
        {
            using var context = CreateContext();
            await context.Songs.AddRangeAsync(BuildSong(1), BuildSong(2));
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Song>(context);

            var song = await repo.GetAsync(2);

            Assert.That(song, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(song!.Id, Is.EqualTo(2));
                Assert.That(song.Title, Is.EqualTo("Title"));
            });
        }

        [Test]
        public async Task GetAsync_UnknownId_ReturnsNull()
        {
            using var context = CreateContext();
            await context.Songs.AddAsync(BuildSong(1));
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Song>(context);

            Assert.That(await repo.GetAsync(999), Is.Null);
        }

        [Test]
        public async Task GetAsync_WithIncludes_LoadsNavigationCollection()
        {
            var dbName = Guid.NewGuid().ToString();

            await using (var seedContext = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options))
            {
                var playlist = new Playlist { Id = 1, Name = "Mix", UserId = 1 };
                playlist.Songs = new List<Song> { BuildSong(10), BuildSong(20) };
                await seedContext.Playlists.AddAsync(playlist);
                await seedContext.SaveChangesAsync();
            }

            await using var queryContext = new AppDbContext(
                new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(dbName).Options);
            var repo = new BaseRepository<Playlist>(queryContext);

            var loaded = await repo.GetAsync(1, new[] { nameof(Playlist.Songs) });

            Assert.Multiple(() =>
            {
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded!.Songs, Is.Not.Null);
                Assert.That(loaded.Songs!.Select(s => s.Id).OrderBy(id => id), Is.EqualTo(new[] { 10, 20 }));
            });
        }

        #endregion

        #region GetAllAsync

        [Test]
        public async Task GetAllAsync_ReturnsEverySeededEntity()
        {
            using var context = CreateContext();
            await context.Playlists.AddRangeAsync(
                new Playlist { Id = 1, Name = "A", UserId = 1 },
                new Playlist { Id = 2, Name = "B", UserId = 2 },
                new Playlist { Id = 3, Name = "C", UserId = 3 });
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Playlist>(context);

            var all = await repo.GetAllAsync();

            Assert.That(all.Select(p => p.Id), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public async Task GetAllAsync_EmptyTable_ReturnsEmptyCollection()
        {
            using var context = CreateContext();
            var repo = new BaseRepository<Playlist>(context);

            Assert.That(await repo.GetAllAsync(), Is.Empty);
        }

        #endregion

        #region GetByIdsAsync

        [Test]
        public async Task GetByIdsAsync_ReturnsOnlyRequestedEntities()
        {
            using var context = CreateContext();
            await context.Songs.AddRangeAsync(BuildSong(1), BuildSong(2), BuildSong(3));
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Song>(context);

            var found = (await repo.GetByIdsAsync(new[] { 3, 1 })).ToList();

            Assert.That(found.Select(s => s.Id).OrderBy(id => id), Is.EqualTo(new[] { 1, 3 }));
        }

        [Test]
        public async Task GetByIdsAsync_DuplicateRequestedIds_ReturnsEachEntityOnce()
        {
            using var context = CreateContext();
            await context.Songs.AddRangeAsync(BuildSong(1), BuildSong(2));
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Song>(context);

            var found = (await repo.GetByIdsAsync(new[] { 1, 1, 1 })).ToList();

            Assert.Multiple(() =>
            {
                Assert.That(found, Has.Count.EqualTo(1));
                Assert.That(found.Single().Id, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task GetByIdsAsync_EmptyInput_ReturnsEmptyWithoutQuerying()
        {
            using var context = CreateContext();
            await context.Songs.AddAsync(BuildSong(1));
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Song>(context);

            Assert.That(await repo.GetByIdsAsync(Array.Empty<int>()), Is.Empty);
        }

        #endregion

        #region FindAsync / Where

        [Test]
        public async Task FindAsync_SingleMatch_ReturnsIt()
        {
            using var context = CreateContext();
            await context.Songs.AddRangeAsync(
                BuildSong(1, "Everlong", "Foo Fighters"),
                BuildSong(2, "Clocks", "Coldplay"));
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Song>(context);

            var song = await repo.FindAsync(s => s.Artist == "Coldplay", Array.Empty<string>());

            Assert.That(song!.Id, Is.EqualTo(2));
        }

        [Test]
        public async Task FindAsync_NoMatch_ReturnsNull()
        {
            using var context = CreateContext();
            await context.Songs.AddAsync(BuildSong(1));
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Song>(context);

            Assert.That(await repo.FindAsync(s => s.Artist == "Nobody", Array.Empty<string>()), Is.Null);
        }

        [Test]
        public async Task Where_FiltersEntitiesLazily()
        {
            using var context = CreateContext();
            await context.Playlists.AddRangeAsync(
                new Playlist { Id = 1, Name = "Mine", UserId = 7 },
                new Playlist { Id = 2, Name = "Theirs", UserId = 8 },
                new Playlist { Id = 3, Name = "Also Mine", UserId = 7 });
            await context.SaveChangesAsync();
            var repo = new BaseRepository<Playlist>(context);

            var mine = await repo.Where(p => p.UserId == 7).ToListAsync();

            Assert.That(mine.Select(p => p.Id).OrderBy(id => id), Is.EqualTo(new[] { 1, 3 }));
        }

        #endregion

        #region Mutations

        [Test]
        public async Task AddOrUpdateAsync_UnkeyedEntity_TracksAsAddedAndPersists()
        {
            using var context = CreateContext();
            var repo = new BaseRepository<Song>(context);
            var song = BuildSong(0, "New Track", "New Artist");

            var returned = await repo.AddOrUpdateAsync(song);
            var stateBeforeSave = context.Entry(song).State;
            await context.SaveChangesAsync();
            var persisted = await context.Songs.SingleAsync(s => s.Id == song.Id);

            Assert.Multiple(() =>
            {
                Assert.That(returned, Is.SameAs(song));
                Assert.That(stateBeforeSave, Is.EqualTo(EntityState.Added));
                Assert.That(song.Id, Is.GreaterThan(0));
                Assert.That(persisted, Is.Not.Null);
            });
        }

        [Test]
        public async Task AddOrUpdateAsync_KeyedDetachedEntity_TracksAsModified()
        {
            using var context = CreateContext();
            var repo = new BaseRepository<Song>(context);
            var song = BuildSong(1, "Original", "Original Artist");
            await context.Songs.AddAsync(song);
            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();

            song.Title = "Renamed";
            await repo.AddOrUpdateAsync(song);

            Assert.That(context.Entry(song).State, Is.EqualTo(EntityState.Modified));
        }

        [Test]
        public async Task AddRangeAsync_PersistsAllEntitiesOnCommit()
        {
            using var context = CreateContext();
            var repo = new BaseRepository<Song>(context);

            var returned = (await repo.AddRangeAsync(new[] { BuildSong(0, "A"), BuildSong(0, "B") })).ToList();
            await context.SaveChangesAsync();
            var total = await context.Songs.CountAsync();

            Assert.Multiple(() =>
            {
                Assert.That(returned, Has.Count.EqualTo(2));
                Assert.That(total, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task Delete_RemovesTrackedEntityOnCommit()
        {
            using var context = CreateContext();
            var repo = new BaseRepository<Playlist>(context);
            var playlist = new Playlist { Id = 1, Name = "Doomed", UserId = 1 };
            await context.Playlists.AddAsync(playlist);
            await context.SaveChangesAsync();

            repo.Delete(playlist);
            await context.SaveChangesAsync();
            var deleted = await repo.GetAsync(1);

            Assert.Multiple(() =>
            {
                Assert.That(context.Entry(playlist).State, Is.EqualTo(EntityState.Detached));
                Assert.That(deleted, Is.Null);
            });
        }

        [Test]
        public void Attach_DetachedEntity_MarksItUnchanged()
        {
            using var context = CreateContext();
            var repo = new BaseRepository<Song>(context);
            var song = BuildSong(5);

            repo.Attach(song);

            Assert.That(context.Entry(song).State, Is.EqualTo(EntityState.Unchanged));
        }

        [Test]
        public async Task Delete_CascadesJoinRowsForManyToManyRelationship()
        {
            using var context = CreateContext();
            var playlist = new Playlist { Id = 1, Name = "Mix", UserId = 1 };
            playlist.Songs = new List<Song> { BuildSong(10) };
            await context.Playlists.AddAsync(playlist);
            await context.SaveChangesAsync();
            var joinCountBefore = await context.Set<PlaylistSong>().CountAsync();

            context.Playlists.Remove(playlist);
            await context.SaveChangesAsync();
            var joinCountAfter = await context.Set<PlaylistSong>().CountAsync();

            Assert.Multiple(() =>
            {
                Assert.That(joinCountBefore, Is.EqualTo(1));
                Assert.That(joinCountAfter, Is.EqualTo(0));
            });
        }

        #endregion
    }
}
