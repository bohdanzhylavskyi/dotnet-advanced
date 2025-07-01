using CommandLine;
using System.Text.RegularExpressions;

namespace Fundametals__FileSystemVisitor_
{
    public enum FilterOption
    {
        GlobPattern,
        FoldersOnly,
        FilesOnly
    }

    public class Options
    {
        [Option('t', "target-path", Required = true, HelpText = "Target folder path")]
        public string TargetPath { get; set; } = "";

        [Option('f', "filter", Required = true, HelpText = "Filter")]
        public FilterOption Filter { get; set; }

        [Option('l', "log-events", Required = false, HelpText = "Log events")]
        public bool LogEvents { get; set; }

        [Option('g', "glob-pattern", Required = false, HelpText = "Glob Pattern")]
        public string GlobPattern { get; set; } = "*";
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            var options = Parser.Default.ParseArguments<Options>(args).WithParsed(Run);
        }

        static void Run(Options options)
        {
            var targetPath = options.TargetPath;
            var filter = ResolveFilter(options);
            var logEvents = options.LogEvents;

            IFileSystemReader fsReader = new FileSystemReader();
            var fsVisitor = new FileSystemVisitor(targetPath, fsReader, filter, logEvents);

            Print(fsVisitor.SearchEntries());
        }

        static void Print(IEnumerable<FileSystemEntry> fsEntries)
        {
            foreach (var entry in fsEntries)
            {
                Console.WriteLine(entry.Path);
            }
        }

        static bool FoldersOnly(FileSystemEntry entry)
        {
            return entry.Type == FileSystemEntryType.Folder;
        }
        
        static bool FilesOnly(FileSystemEntry entry)
        {
            return entry.Type == FileSystemEntryType.File;
        }

        static FileSystemVisitorFilter CreateGlobPatternFilter(string globPattern)
        {
            string regexPattern = "^" + Regex.Escape(globPattern)
               .Replace(@"\*", ".*")
               .Replace(@"\?", ".") + "$";

            return (FileSystemEntry entry) => Regex.IsMatch(entry.Path, regexPattern);
        }

        private static FileSystemVisitorFilter ResolveFilter(Options options)
        {
            switch (options.Filter)
            {
                case FilterOption.FoldersOnly:
                    return FoldersOnly;
                case FilterOption.FilesOnly:
                    return FilesOnly;
                case FilterOption.GlobPattern:
                    return CreateGlobPatternFilter(options.GlobPattern);
                default:
                    throw new InvalidOperationException($"Unknown filter option '{options.Filter}'");
            }
        }
    }
}
