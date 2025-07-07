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
}
