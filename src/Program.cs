using System.Text;
using Pastel;

namespace FCC;

public class FCC
{
    private static string _version = "1.0.2";
    private static StringBuilder _header = new StringBuilder()
        .AppendLine($"FCC # {_version}")
        .AppendLine("-----------------------------");

    public static int Main(string[] args)
    {
        ConsoleExtensions.Disable();
        var arg = new Arguments();

        try
        {
            arg.ParseArgs(args);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{_header.ToString()}{ex.Message}");
            return 1;
        }

        if (arg.Help)
        {
            Console.WriteLine($"{_header.ToString()}{arg.GetHelp()}");
            return 0;
        }

        if (arg.ColorOutput)
        {
            ConsoleExtensions.Enable();
        }

        var frOut = new FolderReader.Output();
        try
        {
            frOut = new FolderReader(arg.Path, GetFlags(arg), arg.MinCharCnt).Analyze();
        }
        catch (Exception)
        {
            Console.WriteLine($"{_header.ToString()}You do not have sufficient permissions to view all directories or files.");
            return 2;
        }

        if (arg.PathToSave is not null)
        {
            var dstr = DateTime.Now.ToString("u").Replace(".", "_").Replace(":", "_");
            var savePath = arg.PathToSave.FullName + $"\\{dstr}.fcc";

            var stats = new StringBuilder()
                .Append($"-----------------------------\nTOTAL: {frOut.Stats.Files} FILES");

            if (!arg.Verbose)
            {
                stats.Append($" | {frOut.Stats.Groups} GROUPS");
            }

            stats.Append($" | {frOut.Stats.Size}");

            Console.WriteLine($"{_header.ToString()}Saved to '{savePath}'\n{stats.ToString()}");
            File.WriteAllTextAsync(savePath, frOut.Result.ToString()).GetAwaiter().GetResult();
            return 0;
        }

        Console.WriteLine($"{_header.ToString()}{frOut.Result.ToString()}");
        return 0;
    }

    private static FolderReader.Configuration GetFlags(Arguments args)
        => FolderReader.Configuration.None
            .SetGroupSize(args.GroupSize)
            .SetDirNames(args.DirNames)
            .SetRecursive(args.Recurse)
            .SetVerbose(args.Verbose)
            .SetRandom(args.Random)
            .SetHidden(args.Hidden);
}