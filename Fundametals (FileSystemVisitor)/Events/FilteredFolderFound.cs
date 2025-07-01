namespace Fundametals__FileSystemVisitor_
{
    public class FilteredFolderFoundEventArgs : EventArgs
    {
        public string Path { get; set; }
        public bool CancelRequested { get; set; }

        public bool ExcludeRequested { get; set; }

        public FilteredFolderFoundEventArgs(string path)
        {
            Path = path;
        }
    }
}
