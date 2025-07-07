namespace Fundametals__FileSystemVisitor_
{
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
}
