using System;
using System.IO;
using System.Linq;

namespace Ryujinx.BuildValidationTasks
{
    public class Program
    {
        static void Main(string[] args)
        {
            // Display the number of command line arguments.
            if (args.Length == 0)
                throw new ArgumentException("Error: too few arguments!");

            string path = args[0];

            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Error: path is null or empty!");

            if (!Path.Exists(path))
                throw new FileLoadException($"path {{{path}}} does not exist!");

            path = Path.GetFullPath(path);

            if (!Directory.GetDirectories(path).Contains($"{path}src"))
                throw new FileLoadException($"path {{{path}}} is not a valid ryujinx project!");

            bool isGitRunner = path.Contains("runner") || path.Contains("D:\\a\\Ryujinx\\Ryujinx");
            if (isGitRunner)
                Console.WriteLine("Is Git Runner!");

            // Run tasks
            // Pass extra info needed in the task constructors
            new LocalesValidationTask().Execute(path, isGitRunner);
        }
    }
}
