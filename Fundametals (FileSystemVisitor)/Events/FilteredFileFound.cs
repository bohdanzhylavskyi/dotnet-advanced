
namespace Fundametals__FileSystemVisitor_
{
    public class FilteredFileFoundEventArgs : EventArgs
    {
        public string Path { get; set; }
        public bool CancelRequested { get; set; }

        public bool ExcludeRequested { get; set; }

        public FilteredFileFoundEventArgs(string path)
        {
            Path = path;
        }
    }
}
