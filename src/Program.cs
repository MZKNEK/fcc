using System.Text;
using Pastel;

namespace FCC;

public class FCC
{
    private static string _version = "1.0.8";
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

        if (arg.ShowVersion)
        {
            Console.WriteLine($"FCC {_version}");
            return 0;
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
            var reader = new FolderReader(arg.Path, GetFlags(arg), arg.MinCharCnt, arg.MaxCntInGroup, arg.MinCntInGroup);

            if (Environment.GetEnvironmentVariable("FCC_COLORS") is not null)
            {
                var colors = new FolderReader.ConsoleColors();
                colors.Q_COLOR = Environment.GetEnvironmentVariable("FCC_QC") ?? colors.Q_COLOR;
                colors.B_COLOR = Environment.GetEnvironmentVariable("FCC_BC") ?? colors.B_COLOR;
                colors.D_COLOR = Environment.GetEnvironmentVariable("FCC_DC") ?? colors.D_COLOR;
                colors.F_COLOR = Environment.GetEnvironmentVariable("FCC_FC") ?? colors.F_COLOR;
                reader.SetColors(colors);
            }

            frOut = reader.Analyze();
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
            .SetAvg(args.GroupSizeAvg)
            .SetVerbose(args.Verbose)
            .SetRandom(args.Random)
            .SetHidden(args.Hidden);
}