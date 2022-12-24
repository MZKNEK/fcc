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
            PathToSave = null;
            DirNames = false;
            TestMode = false;
            Recurse = false;
            Verbose = false;
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

                _parser.AddOption("-t",
                    new(SetTrue(TestMode),
                    "enable test mode",
                    showInHelp: false));

                _parser.AddOption("-d",
                    new(SetTrue(DirNames),
                    "prints names of subdir(only with -r)"));

                _parser.AddOption("-r",
                    new(SetTrue(Recurse),
                    "enable recursive mode"));

                _parser.AddOption("-v",
                    new(SetTrue(Verbose, DirNames),
                    "enable verbose mode"));

                _parser.AddOption("-p",
                    new(SetPathToDirectory,
                    "path to dir",
                    needNextArgument: true));

                _parser.AddOption("--out",
                    new(SetPathToOutDirectory,
                    "path where save output, dir not file",
                    needNextArgument: true));
            }
        }

        public bool Help;
        public bool Recurse;
        public bool Verbose;
        public bool DirNames;
        public bool TestMode;
        public DirectoryInfo? Path;
        public DirectoryInfo? PathToSave;

        public string GetHelp() => _parser!.GetHelp();

        public void ParseArgs(string[] args) => CopyValues(_parser!.Parse(args));

        private void CopyValues(Arguments a)
        {
            Help = a.Help;
            Path = a.Path;
            Recurse = a.Recurse;
            Verbose = a.Verbose;
            DirNames = a.DirNames;
            TestMode = a.TestMode;
            PathToSave = a.PathToSave;
        }

        public override string ToString()
        {
            return $"-h {Help} -d {DirNames} -r {Recurse} -v {Verbose} -t {TestMode} -p {Path?.FullName} --out {PathToSave?.FullName}";
        }
    }
}