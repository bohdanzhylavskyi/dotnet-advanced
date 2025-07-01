namespace Fundametals__FileSystemVisitor_
{
    public enum FileSystemEntryType
    {
        Folder,
        File,
    }

    public struct FileSystemEntry
    {
        public readonly FileSystemEntryType Type;
        public readonly string Path;

        public FileSystemEntry(FileSystemEntryType type, string path)
        {
            this.Type = type;
            this.Path = path;
        }
    }

    public delegate bool FileSystemVisitorFilter(FileSystemEntry entry);

    public interface IFileSystemReader
    {
        public List<FileSystemEntry> scan(string targetFolderPath);
    }

    public class FileSystemReader : IFileSystemReader
    {
        public List<FileSystemEntry> scan(string targetFolderPath)
        {
            var folders = Directory.GetDirectories(targetFolderPath, "*", SearchOption.AllDirectories)
                            .Select((path) => new FileSystemEntry(FileSystemEntryType.Folder, path)).ToList();

            var files = Directory.GetFiles(targetFolderPath, "*", SearchOption.AllDirectories)
                .Select((path) => new FileSystemEntry(FileSystemEntryType.File, path)).ToList();

            var entries = folders.Concat(files).ToList();

            return entries;
        }
    }

    public class MockedFileSystemReader : IFileSystemReader
    {
        public List<FileSystemEntry> scan(string targetFolderPath)
        {
            return new List<FileSystemEntry>()
            {
                new FileSystemEntry(FileSystemEntryType.File, "x")
            };
        }
    }

    public class FileSystemVisitor
    {
        private readonly string TargetFolderPath;
        private readonly FileSystemVisitorFilter? Filter;
        private readonly IFileSystemReader FsReader;
        private readonly bool LogEvents = false;

        public event EventHandler<FileFoundEventArgs>? FileFound;
        public event EventHandler<FolderFoundEventArgs>? FolderFound;
        public event EventHandler<FilteredFileFoundEventArgs>? FilteredFileFound;
        public event EventHandler<FilteredFolderFoundEventArgs>? FilteredFolderFound;
        public event EventHandler<SearchStartArgs>? SearchStarted;
        public event EventHandler? SearchFinished;

        public FileSystemVisitor(string targetFolderPath, IFileSystemReader fsReader)
        {
            this.TargetFolderPath = targetFolderPath;
            this.FsReader = fsReader;
        }

        public FileSystemVisitor(
            string targetFolderPath,
            IFileSystemReader fsReader,
            FileSystemVisitorFilter filter,
            bool logEvents = false): this(targetFolderPath, fsReader)
        {
            this.Filter = filter;
            this.LogEvents = logEvents;
        }


        public IEnumerable<FileSystemEntry> GetAllEntries()
        {
            return ReadEntries();
        }

        public IEnumerable<FileSystemEntry> SearchEntries()
        {
            if (this.Filter is null)
            {
                throw new Exception("Filter must be configured for search operation");
            }

            FireSearchStartedEvent();

            var allEntries = ReadEntries();
            var entriesToProcess = new List<FileSystemEntry>();

            foreach (var entry in allEntries)
            {
                this.FireFileSystemEntryFound(entry, out bool excluded);

                if (!excluded)
                {
                    entriesToProcess.Add(entry);
                }
            }

            foreach (var entry in entriesToProcess)
            {
                if (!this.Filter(entry))
                {
                    continue;
                }

                this.FireFilteredFileSystemEntryFound(entry, out bool excluded);

                if (!excluded)
                {
                    yield return entry;
                }
            }

            this.FireSearchFinishedEvent();
        }

        private void FireSearchStartedEvent()
        {
            var searchStartArgs = new SearchStartArgs();

            PrintEventLogIfEnabled(FormatEventLog($"SearchStarted, Target Folder: {this.TargetFolderPath}"));

            SearchStarted?.Invoke(this, searchStartArgs);

            if (searchStartArgs.CancelRequested)
            {
                ThrowSearchCancelledException();
            }
        }

        private void FireSearchFinishedEvent()
        {
            PrintEventLogIfEnabled(FormatEventLog("SearchFinished"));
            SearchFinished?.Invoke(this, EventArgs.Empty);
        }

        private void FireFileSystemEntryFound(FileSystemEntry entry, out bool excluded)
        {
            if (entry.Type == FileSystemEntryType.File)
            {
                this.FireFileFoundEvent(entry, out excluded);

                return;
            }

            if (entry.Type == FileSystemEntryType.Folder)
            {
                this.FireFolderFoundEvent(entry, out excluded);

                return;
            }

            excluded = false;

        }

        private void FireFileFoundEvent(FileSystemEntry file, out bool excluded)
        {
            var eventArgs = new FileFoundEventArgs(file.Path);

            PrintEventLogIfEnabled(FormatEventLog($"FileFound: {file.Path}"));

            this.FileFound?.Invoke(this, eventArgs);

            if (eventArgs.CancelRequested)
            {
                ThrowSearchCancelledException();
            }

            excluded = eventArgs.ExcludeRequested;
        }

        private void FireFolderFoundEvent(FileSystemEntry file, out bool excluded)
        {
            var eventArgs = new FolderFoundEventArgs(file.Path);

            PrintEventLogIfEnabled(FormatEventLog($"FolderFound: {file.Path}"));

            this.FolderFound?.Invoke(this, eventArgs);

            if (eventArgs.CancelRequested)
            {
                ThrowSearchCancelledException();
            }

            excluded = eventArgs.ExcludeRequested;
        }

        private void FireFilteredFileSystemEntryFound(FileSystemEntry entry, out bool excluded)
        {
            if (entry.Type == FileSystemEntryType.File)
            {
                this.FireFilteredFileFoundEvent(entry, out excluded);

                return;
            }

            if (entry.Type == FileSystemEntryType.Folder)
            {
                this.FireFilteredFolderFoundEvent(entry, out excluded);

                return;
            }

            excluded = false;

        }

        private void FireFilteredFileFoundEvent(FileSystemEntry file, out bool excluded)
        {
            var eventArgs = new FilteredFileFoundEventArgs(file.Path);

            PrintEventLogIfEnabled(FormatEventLog($"FilteredFileFound: {file.Path}"));

            this.FilteredFileFound?.Invoke(this, eventArgs);

            if (eventArgs.CancelRequested)
            {
                ThrowSearchCancelledException();
            }

            excluded = eventArgs.ExcludeRequested;
        }

        private void FireFilteredFolderFoundEvent(FileSystemEntry file, out bool excluded)
        {
            var eventArgs = new FilteredFolderFoundEventArgs(file.Path);

            PrintEventLogIfEnabled(FormatEventLog($"FilteredFolderFound: {file.Path}"));

            this.FilteredFolderFound?.Invoke(this, eventArgs);

            if (eventArgs.CancelRequested)
            {
                ThrowSearchCancelledException();
            }

            excluded = eventArgs.ExcludeRequested;
        }

        private List<FileSystemEntry> ReadEntries()
        {
            return this.FsReader.scan(this.TargetFolderPath);


            //var folders = Directory.GetDirectories(this.TargetFolderPath, "*", SearchOption.AllDirectories)
            //                .Select((path) => new FileSystemEntry(FileSystemEntyType.Folder, path)).ToList();

            //var files = Directory.GetFiles(this.TargetFolderPath, "*", SearchOption.AllDirectories)
            //    .Select((path) => new FileSystemEntry(FileSystemEntyType.File, path)).ToList();

            //var entries = folders.Concat(files).ToList();

            //return entries;
        }

        private static void ThrowSearchCancelledException()
        {
            throw new Exception("Search operation has been canceled");
        }

        private static string FormatEventLog(string log)
        {
            return $"[EVENT] {log}";
        }

        private static void PrintEventLogIfEnabled(string log)
        {
            Console.WriteLine(log);
        }
    }
}
