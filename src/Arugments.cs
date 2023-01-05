using System.Runtime.CompilerServices;
using SCLIAP;

namespace FCC
{
    internal class Arguments
    {
        private static SimpleCLIArgsParser<Arguments>? _parser = null;

        private static OptionInfo<Arguments>.OptionAction SetPathToDirectory = (a, s) =>
        {
            if (!Directory.Exists(s))
            {
                throw new Exception($"Directory '{s}' don't exist!");
            }
            a.Path = new DirectoryInfo(s);
            a.Help = false;
        };

        private static OptionInfo<Arguments>.OptionAction SetPathToOutDirectory = (a, s) =>
        {
            if (!Directory.Exists(s))
            {
                throw new Exception($"Directory '{s}' don't exist!");
            }
            a.PathToSave = new DirectoryInfo(s);
            a.ColorOutput = false;
        };

        private static void SetTrue(Arguments args, string name)
        {
            var arg = args.GetType().GetField(name);
            if (arg is not null)
                arg.SetValue(args, true);
        }

        private OptionInfo<Arguments>.OptionAction SetTrue(object a1, object? a2 = null,
            [CallerArgumentExpression("a1")]string a1N = "", [CallerArgumentExpression("a2")]string a2N = "")
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
            ColorOutput = false;
            PathToSave = null;
            DirNames = false;
            Recurse = false;
            Verbose = false;
            Hidden = false;
            Help = true;
            Path = null;
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
                    new(SetTrue(DirNames),
                    "prints names of subdir(only with -r)",
                    name: 'd'));

                _parser.AddOption(
                    new(SetTrue(Recurse),
                    "enable recursive mode",
                    name: 'r'));

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
            }
        }

        public bool Help;
        public bool Hidden;
        public bool Recurse;
        public bool Verbose;
        public bool DirNames;
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
            Recurse = a.Recurse;
            Verbose = a.Verbose;
            DirNames = a.DirNames;
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
                    -v {Verbose}
                    -a {Hidden}
                    -p {Path?.FullName}
                    --out {PathToSave?.FullName}
                    """;
        }
    }
}