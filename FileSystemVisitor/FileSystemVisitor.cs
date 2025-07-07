namespace Fundametals__FileSystemVisitor_
{
    public delegate bool FileSystemVisitorFilter(FileSystemEntry entry);

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
            VerifyTargetFolderExists();

            return ReadEntries();
        }

        public IEnumerable<FileSystemEntry> SearchEntries()
        {
            VerifyTargetFolderExists();

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

        private void VerifyTargetFolderExists()
        {
            if (!FsReader.FolderExists(this.TargetFolderPath))
            {
                throw new InvalidOperationException("Target folder does not exist");
            }
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
            return this.FsReader.Scan(this.TargetFolderPath);
        }

        private static void ThrowSearchCancelledException()
        {
            throw new Exception("Search operation has been canceled");
        }

        private static string FormatEventLog(string log)
        {
            return $"[EVENT] {log}";
        }

        private void PrintEventLogIfEnabled(string log)
        {
            if (LogEvents)
            {
                Console.WriteLine(log);
            }
        }
    }
}
