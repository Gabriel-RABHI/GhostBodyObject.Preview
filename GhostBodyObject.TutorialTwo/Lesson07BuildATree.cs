using GhostBodyObject.Runtime.Logical.Body;
using GhostBodyObject.Runtime.Logical.Model;
using GhostBodyObject.Runtime.Physical;
using GhostBodyObject.Runtime.Physical.Constants;

namespace TutorialTwo.Trees
{
    using static LibraryRepositoryCollections;

    // The third model: the library of a listener, its playlists in folders, the folders in folders. A folder is a part of two
    // types of Owner: the listener, for a folder at the top, or another folder.

    [Entity(1, 1, 2), Root, Removable]
    internal interface MListener
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MFolder> Folders { get; }
    }

    [Entity(2, 1, 3), Enumerable]
    internal interface MFolder
    {
        [Property(1)]
        VName Name { get; set; }

        [Property(2)]
        IEntityCollection<MFolder> Folders { get; }

        [Property(3)]
        IEntityCollection<MPlaylist> Playlists { get; }

        // Two types compose a folder: each [OwnedFrom] views the Owner of one type, an Option. Exactly one is present.
        [OwnedFrom]
        Option<MListener> Listener { get; }

        [OwnedFrom]
        Option<MFolder> Parent { get; }
    }

    [Entity(3, 1, 1), Enumerable]
    internal interface MPlaylist
    {
        [Property(1)]
        VName Name { get; set; }

        [OwnedFrom]
        MFolder Folder { get; }
    }

    [GhostRepository(2), IncludeEntityModel(nameof(MListener)), IncludeEntityModel(nameof(MFolder)), IncludeEntityModel(nameof(MPlaylist))]
    internal interface MLibraryRepository
    {
    }

    /// <summary>
    /// Lesson 7: build a tree.
    /// <para>
    /// A part may have several types of Owner: a folder lives in a listener, at the top of its library, or in another folder.
    /// The model says it by the compositions themselves: <c>MListener.Folders</c> and <c>MFolder.Folders</c>; the folder reads
    /// its Owner by an <c>[OwnedFrom] Option&lt;T&gt;</c> per type, exactly one of which is present. A chain of Owners always
    /// leads to a Root: the generator checks it (error GBO006 otherwise).
    /// </para>
    /// <para>
    /// A tree has no cycle: <c>MoveTo</c> refuses to place a folder under itself, or under one of its subfolders, before it
    /// changes anything. The removal of a folder removes its whole subtree.
    /// </para>
    /// </summary>
    public sealed class Lesson07BuildATree
    {
        /// <summary>Folders in a library and in folders; each one reads its Owner of the right type.</summary>
        [Fact]
        public void BuildATreeOfFolders()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-tree", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Listener { Name = "Ada" };
            var music = ada.Folders.New();
            music.Name = "Music";
            var jazz = music.Folders.New();
            jazz.Name = "Jazz";
            jazz.Playlists.New().Name = "Late night";

            Assert.Same(ada, music.Listener.Get());
            Assert.False(music.Parent.Any());
            Assert.Same(music, jazz.Parent.Get());
            Assert.False(jazz.Listener.Any());
            Assert.Equal("Jazz", jazz.Playlists.Single().Folder.Name);
            transaction.Commit();
        }

        /// <summary>A folder moves under another one, never under itself nor its subfolders; removed, it takes its subtree.</summary>
        [Fact]
        public void MoveAndRemoveASubtree()
        {
            using var store = TransactionalMemoryStore.NewInMemoryRepositoryStore("lesson-07-move", StoreAccessMode.ReadWrite);
            using var library = new LibraryRepository(store);
            using var transaction = library.OpenTransaction(StoreAccessMode.ReadWrite);
            var ada = new Listener { Name = "Ada" };
            var music = ada.Folders.New();
            var jazz = music.Folders.New();
            var bebop = jazz.Folders.New();
            bebop.Playlists.New();
            var archive = ada.Folders.New();

            // Under its own subfolder: a cycle, refused before anything changes.
            Assert.Throws<InvalidOperationException>(() => music.MoveTo(bebop.Folders));
            Assert.Same(ada, music.Listener.Get());

            // From the top of the library into a folder: its Owner changes of type.
            music.MoveTo(archive.Folders);
            Assert.Same(archive, music.Parent.Get());
            Assert.False(music.Listener.Any());

            // The archive removed, with its whole subtree.
            ada.Folders.Remove(archive);
            Assert.Equal((0, 0), (FolderCollection.Count(), PlaylistCollection.Count()));
            transaction.Commit();
        }
    }
}
