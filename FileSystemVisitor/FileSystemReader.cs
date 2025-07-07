namespace Fundametals__FileSystemVisitor_
{
    public interface IFileSystemReader
    {
        public List<FileSystemEntry> Scan(string folderPath);
        public bool FolderExists(string folderPath);
    }

    public class FileSystemReader : IFileSystemReader
    {
        public List<FileSystemEntry> Scan(string folderPath)
        {
            var folders = Directory.GetDirectories(folderPath, "*", SearchOption.AllDirectories)
                            .Select((path) => new FileSystemEntry(FileSystemEntryType.Folder, path)).ToList();

            var files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Select((path) => new FileSystemEntry(FileSystemEntryType.File, path)).ToList();

            var entries = folders.Concat(files).ToList();

            return entries;
        }

        public bool FolderExists(string folderPath)
        {
            return Directory.Exists(folderPath);
        }
    }
}
