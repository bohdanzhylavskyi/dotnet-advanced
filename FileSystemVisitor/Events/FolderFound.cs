namespace Fundametals__FileSystemVisitor_
{
    public class FolderFoundEventArgs : EventArgs
    {
        public string Path { get; set; }
        public bool CancelRequested { get; set; }
        public bool ExcludeRequested { get; set; }

        public FolderFoundEventArgs(string path)
        {
            Path = path;
        }
    }
}
