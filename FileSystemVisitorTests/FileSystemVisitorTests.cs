using Fundametals__FileSystemVisitor_;
using Moq;

namespace TestProject1
{
    public interface IEventsHandler
    {
        public void HandleSearchStartedEvent(object sender, SearchStartArgs args);
        public void HandleSearchFinishedEvent(object sender, EventArgs args);
        public void HandleFileFoundEvent(object sender, FileFoundEventArgs args);
        public void HandleFolderFoundEvent(object sender, FolderFoundEventArgs args);
        public void HandleFilteredFileFoundEvent(object sender, FilteredFileFoundEventArgs args);
        public void HandleFilteredFolderFoundEvent(object sender, FilteredFolderFoundEventArgs args);
    }

    [TestClass]
    public sealed class FileSystemVisitorTests
    {
        private string TargetFolder = "C:/documents";
        private Mock<IEventsHandler> EventsHandler { get; set; }

        private const string SearchCanceledExceptionMessage = "Search operation has been canceled";

        [TestInitialize()]
        public void Startup()
        {
            EventsHandler = new Mock<IEventsHandler>();
        }

        [TestMethod]
        public void GetAllEntries_ReturnsCorrectResults()
        {
            var fsReaderMock = this.ConfigureFsReaderMock();

            var fsVisitor = new FileSystemVisitor(TargetFolder, fsReaderMock.Object);

            Assert.AreEqual(fsVisitor.GetAllEntries().Count(), fsReaderMock.Object.scan(TargetFolder).Count());
        }

        [TestMethod]
        public void SearchEntries_ReturnsCorrectResults()
        {
            var fsReaderMock = this.ConfigureFsReaderMock();

            FileSystemVisitorFilter filter = (entry) => entry.Path.Contains("file2.txt");

            var fsVisitor = new FileSystemVisitor(TargetFolder, fsReaderMock.Object, filter);
            var result = fsVisitor.SearchEntries().ToList();

            Assert.AreEqual(2, result.Count());
            Assert.AreEqual($"{TargetFolder}/file2.txt", result[0].Path);
            Assert.AreEqual($"{TargetFolder}/subfolder/file2.txt", result[1].Path);
        }

        [TestMethod]
        public void SearchEntries_EventsInvokedCorrectly()
        {
            var fsReaderMock = this.ConfigureFsReaderMock();

            var fsVisitor = new FileSystemVisitor(
                TargetFolder,
                fsReaderMock.Object,
                (entry) => entry.Path.Contains("file2.txt")
            );

            SubscribeToEvents(fsVisitor, EventsHandler.Object);

            var result = fsVisitor.SearchEntries().ToList();

            EventsHandler.Verify(
                instance => instance.HandleSearchStartedEvent(
                    It.IsAny<object>(),
                    It.IsAny<SearchStartArgs>()
                ), Times.Exactly(1));

            EventsHandler.Verify(instance => instance.HandleFileFoundEvent(
                It.IsAny<object>(),
                It.Is<FileFoundEventArgs>(args => args.Path.EndsWith("/file1.txt")
            )));

            EventsHandler.Verify(instance => instance.HandleFileFoundEvent(
                It.IsAny<object>(),
                It.Is<FileFoundEventArgs>(args => args.Path.EndsWith("/subfolder/file2.txt")
            )));

            EventsHandler.Verify(
                instance => instance.HandleFilteredFileFoundEvent(
                    It.IsAny<object>(),
                    It.Is<FilteredFileFoundEventArgs>(args => args.Path == $"{TargetFolder}/file2.txt")
                ),
                Times.Exactly(1)
            );
            EventsHandler.Verify(
                instance => instance.HandleFilteredFileFoundEvent(
                    It.IsAny<object>(),
                    It.Is<FilteredFileFoundEventArgs>(args => args.Path == $"{TargetFolder}/subfolder/file2.txt")
                ),
                Times.Exactly(1)
            );

            EventsHandler.Verify(instance => instance.HandleSearchFinishedEvent(
                It.IsAny<object>(),
                It.IsAny<EventArgs>()
            ));
        }

        [TestMethod]
        public void SearchEntries_SearchCancelled_SearchCancelledExceptionThrown()
        {
            var eventsHandler = new Mock<IEventsHandler>();
            var fsReaderMock = this.ConfigureFsReaderMock();

            eventsHandler.Setup(
                instance => instance.HandleSearchStartedEvent(
                    It.IsAny<object>(),
                    It.IsAny<SearchStartArgs>())
            ).Callback((object sender, SearchStartArgs args) =>
            {
                args.CancelRequested = true;
            });

            var fsVisitor = new FileSystemVisitor(
                TargetFolder,
                fsReaderMock.Object,
                (entry) => entry.Path.Contains("file2.txt")
            );

            SubscribeToEvents(fsVisitor, eventsHandler.Object);

            var ex = Assert.ThrowsException<Exception>(() => fsVisitor.SearchEntries().ToList());

            Assert.AreEqual(SearchCanceledExceptionMessage, ex.Message);
        }

        [TestMethod]
        public void SearchEntries_IgnoredEntriesAreNotPresentedInResultingList()
        {
            var eventsHandler = new Mock<IEventsHandler>();
            var fsReaderMock = this.ConfigureFsReaderMock();

            eventsHandler.Setup(
                instance => instance.HandleFilteredFileFoundEvent(
                    It.IsAny<object>(),
                    It.Is<FilteredFileFoundEventArgs>((args) => args.Path.Contains("file2.txt"))
                )
            ).Callback((object _, FilteredFileFoundEventArgs args) => args.ExcludeRequested = true);

            eventsHandler.Setup(
                instance => instance.HandleFilteredFolderFoundEvent(
                    It.IsAny<object>(),
                    It.Is<FilteredFolderFoundEventArgs>((args) => args.Path == $"{TargetFolder}/subfolder")
                )
            ).Callback((object _, FilteredFolderFoundEventArgs args) => args.ExcludeRequested = true);

            var fsVisitor = new FileSystemVisitor(
                TargetFolder,
                fsReaderMock.Object,
                (entry) =>
                {
                    return entry.Type == FileSystemEntryType.Folder
                        ? entry.Path.EndsWith("subfolder")
                        : entry.Path.Contains("file2.txt");
                }
            );

            SubscribeToEvents(fsVisitor, eventsHandler.Object);

            var result = fsVisitor.SearchEntries().ToList();

            Assert.AreEqual(0, result.Count());
        }

        private FileSystemEntry CreateFileEntry(string path)
        {
            return new FileSystemEntry(FileSystemEntryType.File, path);
        }

        private FileSystemEntry CreateFolderEntry(string path)
        {
            return new FileSystemEntry(FileSystemEntryType.Folder, path);
        }

        private void SubscribeToEvents(FileSystemVisitor fsVisitor, IEventsHandler eventsHandler)
        {
            fsVisitor.SearchStarted += eventsHandler.HandleSearchStartedEvent;
            fsVisitor.SearchFinished += eventsHandler.HandleSearchFinishedEvent;
            fsVisitor.FileFound += eventsHandler.HandleFileFoundEvent;
            fsVisitor.FolderFound += eventsHandler.HandleFolderFoundEvent;
            fsVisitor.FilteredFileFound += eventsHandler.HandleFilteredFileFoundEvent;
            fsVisitor.FilteredFolderFound += eventsHandler.HandleFilteredFolderFoundEvent;
        }

        private Mock<IFileSystemReader> ConfigureFsReaderMock()
        {
            var fsReaderMock = new Mock<IFileSystemReader>();

            var mockedFsEntries = new List<FileSystemEntry>()
            {
                CreateFileEntry($"{TargetFolder}/file1.txt"),
                CreateFileEntry($"{TargetFolder}/file2.txt"),
                CreateFileEntry($"{TargetFolder}/file3.txt"),
                CreateFileEntry($"{TargetFolder}/subfolder/file1.txt"),
                CreateFileEntry($"{TargetFolder}/subfolder/file2.txt"),

                CreateFolderEntry($"{TargetFolder}/subfolder")
            };

            fsReaderMock.Setup((f) => f.scan(TargetFolder))
                        .Returns(mockedFsEntries);

            return fsReaderMock;
        }
    }
}
