using System.Runtime.CompilerServices;
using SCLIAP;

namespace FCC;

internal class Arguments
{
    private static SimpleCLIArgsParser<Arguments>? _parser = null;

    private static Action<Arguments, string> SetPathToDirectory = (a, s) =>
    {
        if (!Directory.Exists(s))
        {
            throw new Exception($"Directory '{s}' don't exist!");
        }
        a.Path = new DirectoryInfo(s);
    };

    private static Action<Arguments, string> SetPathToOutDirectory = (a, s) =>
    {
        if (!Directory.Exists(s))
        {
            throw new Exception($"Directory '{s}' don't exist!");
        }
        a.PathToSave = new DirectoryInfo(s);
        a.ColorOutput = false;
    };

    private static Action<Arguments, string> SetMinCharCnt = (a, s) =>
    {
        if (!uint.TryParse(s, out a.MinCharCnt))
        {
            throw new Exception($"Min: '{s}' isn't a positive number!");
        }
    };

    private static void SetTrue(Arguments args, string name)
    {
        var arg = args.GetType().GetField(name);
        if (arg is not null)
            arg.SetValue(args, true);
    }

    private Action<Arguments, string> SetTrue(object a1, object? a2 = null,
        [CallerArgumentExpression("a1")] string a1N = "",[CallerArgumentExpression("a2")] string a2N = "")
    {
        return (a, s) =>
        {
            SetTrue(a, a1N);
            if (a2 is not null)
                SetTrue(a, a2N);
        };
    }

    public Arguments()
    {
        Path = new DirectoryInfo(".");
        ColorOutput = false;
        PathToSave = null;
        GroupSize = false;
        DirNames = false;
        Recurse = false;
        Verbose = false;
        MinCharCnt = 17;
        Hidden = false;
        Random = false;
        Help = false;
        Init();
    }

    private void Init()
    {
        if (_parser is null)
        {
            _parser = new();
            _parser.AddDefaultHelpOptions(SetTrue(Help));

            _parser.AddOption(
                new(SetTrue(Hidden),
                "include hidden directories",
                name: 'a'));

            _parser.AddOption(
                new(SetTrue(ColorOutput),
                "enable colored output",
                name: 'c'));

            _parser.AddOption(
                new(SetTrue(GroupSize),
                "include group size in output",
                name: 's'));

            _parser.AddOption(
                new(SetTrue(DirNames),
                "prints names of subdir(only with -r)",
                name: 'd'));

            _parser.AddOption(
                new(SetTrue(Recurse),
                "enable recursive mode",
                name: 'r'));

            _parser.AddOption(
                new(SetTrue(Random),
                "get one random entry",
                longName: "rand"));

            _parser.AddOption(
                new(SetTrue(Verbose, DirNames),
                "enable verbose mode",
                name: 'v'));

            _parser.AddOption(
                new(SetPathToDirectory,
                "path to dir",
                needNextArgument: true,
                name: 'p'));

            _parser.AddOption(
                new(SetPathToOutDirectory,
                "path where save output, dir not file",
                needNextArgument: true,
                longName: "out"));

            _parser.AddOption(
                new(SetMinCharCnt,
                "min length of short name",
                needNextArgument: true,
                longName: "min"));
        }
    }

    public bool Help;
    public bool Hidden;
    public bool Random;
    public bool Recurse;
    public bool Verbose;
    public bool DirNames;
    public bool GroupSize;
    public uint MinCharCnt;
    public bool ColorOutput;
    public DirectoryInfo? Path;
    public DirectoryInfo? PathToSave;

    public string GetHelp() => _parser!.GetHelp();

    public void ParseArgs(string[] args) => CopyValues(_parser!.Parse(args));

    private void CopyValues(Arguments a)
    {
        Help = a.Help;
        Path = a.Path;
        Hidden = a.Hidden;
        Random = a.Random;
        Recurse = a.Recurse;
        Verbose = a.Verbose;
        DirNames = a.DirNames;
        GroupSize = a.GroupSize;
        MinCharCnt = a.MinCharCnt;
        PathToSave = a.PathToSave;
        ColorOutput = a.ColorOutput;
    }

    public override string ToString()
    {
        return $"""
                -h {Help}
                -d {DirNames}
                -r {Recurse}
                -c {ColorOutput}
                -s {GroupSize}
                -v {Verbose}
                -a {Hidden}
                -p {Path?.FullName}
                --rand {Random}
                --min {MinCharCnt}
                --out {PathToSave?.FullName}
                """;
    }
}