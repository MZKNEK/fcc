using System.Text;
using Pastel;

namespace FCC;

public class FCC
{
    private static readonly string _version =
        typeof(FCC).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    private static readonly StringBuilder _header = new StringBuilder()
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
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine($"{_header.ToString()}You do not have sufficient permissions to view all directories or files.");
            return 2;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"{_header.ToString()}I/O error: {ex.Message}");
            return 3;
        }

        if (arg.PathToSave is not null)
        {
            var dstr = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var savePath = Path.Combine(arg.PathToSave.FullName, $"{dstr}.fcc");

            try
            {
                File.WriteAllText(savePath, frOut.Result.ToString());
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Console.WriteLine($"{_header.ToString()}Could not save output: {ex.Message}");
                return 3;
            }

            var stats = new StringBuilder()
                .AppendLine("-----------------------------")
                .AppendLine($"TOTAL: {frOut.Stats.Summary(!arg.Verbose)}");

            Console.WriteLine($"{_header.ToString()}Saved to '{savePath}'\n{stats.ToString()}");
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