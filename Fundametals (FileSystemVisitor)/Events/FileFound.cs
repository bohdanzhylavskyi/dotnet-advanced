
namespace Fundametals__FileSystemVisitor_
{
    public class FileFoundEventArgs : EventArgs
    {
        public string Path { get; set; }
        public bool CancelRequested { get; set; }

        public bool ExcludeRequested { get; set; }

        public FileFoundEventArgs(string path)
        {
            Path = path;
        }
    }
}
