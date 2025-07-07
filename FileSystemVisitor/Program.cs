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

        private static FileSystemVisitorFilter ResolveFilter(Options options)
        {
            var globPattern = options.GlobPattern;

            switch (options.Filter)
            {
                case FilterOption.FoldersOnly:
                    return CreateFoldersOnlyFilter(globPattern);
                case FilterOption.FilesOnly:
                    return CreateFilesOnlyFilter(globPattern);
                case FilterOption.GlobPattern:
                    return CreateGlobPatternFilter(globPattern);
                default:
                    throw new InvalidOperationException($"Unknown filter option '{options.Filter}'");
            }
        }

        private static FileSystemVisitorFilter CreateFoldersOnlyFilter(string globPattern)
        {
            return (FileSystemEntry entry) =>
                entry.Type == FileSystemEntryType.Folder
                && IsPathMatchesGlob(entry.Path, globPattern);
        }

        private static FileSystemVisitorFilter CreateFilesOnlyFilter(string globPattern)
        {
            return (FileSystemEntry entry) =>
                entry.Type == FileSystemEntryType.File
                && IsPathMatchesGlob(entry.Path, globPattern);
        }

        private static FileSystemVisitorFilter CreateGlobPatternFilter(string globPattern)
        {
            return (FileSystemEntry entry) => IsPathMatchesGlob(entry.Path, globPattern);
        }

        private static bool IsPathMatchesGlob(string path, string globPattern)
        {
            string regexPattern = "^" + Regex.Escape(globPattern)
               .Replace(@"\*", ".*")
               .Replace(@"\?", ".") + "$";

            return Regex.IsMatch(path, regexPattern);
        }
    }
}
