using System.Text;

namespace FCC
{
    public class FCC
    {
        private static string _version = "0.0.3";
        private static StringBuilder _header = new StringBuilder()
            .AppendLine($"FCC # {_version}")
            .AppendLine("-----------------------------");

        public static int Main(string[] args)
        {
            var arg = new Arguments();
            try
            {
                arg.ParseArgs(args);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{_header.ToString()}{ex.Message}");
                return  1;
            }

            if (arg.TestMode)
            {
                Console.WriteLine($"{_header.ToString()}ARGS: {string.Join(" ", args)}\nP: {arg}");
                return 0;
            }

            if (arg.Help)
            {
                Console.WriteLine($"{_header.ToString()}{arg.GetHelp()}");
                return 0;
            }

            var frOut = new FolderReader.Output();
            try
            {
                frOut = new FolderReader(arg.Verbose, arg.Recurse, arg.DirNames, arg.Path).Analyze();
            }
            catch(Exception)
            {
                Console.WriteLine($"{_header.ToString()}You do not have sufficient permissions to view all directories or files.");
                return 2;
            }

            if (arg.PathToSave is not null)
            {
                var dstr = DateTime.Now.ToString("u").Replace(".", "_").Replace(":", "_");
                var savePath = arg.PathToSave.FullName + $"\\{dstr}.fcc";

                var stats = new StringBuilder()
                    .Append($"-----------------------------\nTOTAL: {frOut.Stas.Files} FILES");

                if (!arg.Verbose)
                {
                    stats.Append($" | {frOut.Stas.Groups} GROUPS");
                }

                Console.WriteLine($"{_header.ToString()}Saved to '{savePath}'\n{stats.ToString()}");
                File.WriteAllTextAsync(savePath, frOut.Result.ToString()).GetAwaiter().GetResult();
                return 0;
            }

            Console.WriteLine($"{_header.ToString()}{frOut.Result.ToString()}");

            return 0;
        }
    }
}