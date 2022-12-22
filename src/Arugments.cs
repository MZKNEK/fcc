namespace FCC
{
    internal class Arguments
    {
        private struct Param
        {
            public string Desc;
            public bool HideInHelp;
            public bool HasMoreParams;
            public Action<Arguments, string> Method;

            public Param(Action<Arguments, string> method, bool hmp, string desc, bool hide = false)
            {
                Desc = desc;
                Method = method;
                HideInHelp = hide;
                HasMoreParams = hmp;
            }
        }

        private static Action<Arguments, string> SetPathToDirectory = (a, s) =>
        {
            if (!Directory.Exists(s))
            {
                throw new Exception($"Directory '{s}' don't exist!");
            }
            a.Path = new DirectoryInfo(s);
            a.Help = false;
        };

        private static Action<Arguments, string> SetPathToOutDirectory = (a, s) =>
        {
            if (!Directory.Exists(s))
            {
                throw new Exception($"Directory '{s}' don't exist!");
            }
            a.PathToSave = new DirectoryInfo(s);
        };

        private static Action<Arguments, string> SetVerboseToTrue = (a, s) =>
        {
            a.Verbose = true;
            a.DirNames = true;
        };

        private static Action<Arguments, string> SetHelpToTrue = (a, s) =>
        {
            a.Help = true;
        };

        private static Action<Arguments, string> SetDirNamesToTrue = (a, s) =>
        {
            a.DirNames = true;
        };

        private static Action<Arguments, string> SetRecurseToTrue = (a, s) =>
        {
            a.Recurse = true;
        };

        private static Action<Arguments, string> SetTestModeToTrue = (a, s) =>
        {
            a.TestMode = true;
        };

        private static readonly Dictionary<string, Param> _args = new Dictionary<string, Param>
        {
            {"-h",    new(SetHelpToTrue,           false, "prints help"                             )},
            {"-d",    new(SetDirNamesToTrue,       false, "prints names of subdir(only with -r)"    )},
            {"-r",    new(SetRecurseToTrue,        false, "enable recursive mode"                   )},
            {"-v",    new(SetVerboseToTrue,        false, "enable verbose mode(also sets -d)"       )},
            {"-t",    new(SetTestModeToTrue,       false, "enable test mode",                   true)},
            {"-p",    new(SetPathToDirectory,      true,  "path to dir"                             )},
            {"--out", new(SetPathToOutDirectory,   true,  "path where save output, dir not file"    )},
        };

        private static readonly string _help = string.Join("\n", _args.Where(x => !x.Value.HideInHelp).Select(x => $"{x.Key}\t{x.Value.Desc}"));

        internal Arguments()
        {
            PathToSave = null;
            DirNames = false;
            TestMode = false;
            Recurse = false;
            Verbose = false;
            Help = true;
            Path = null;
        }

        internal bool Help;
        internal bool Recurse;
        internal bool Verbose;
        internal bool DirNames;
        internal bool TestMode;
        internal DirectoryInfo? Path;
        internal DirectoryInfo? PathToSave;

        internal string GetHelp() => _help;

        internal void ParseArgs(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                var nextArg = (i+1 >= args.Length)
                    ? string.Empty
                    : args[i+1];

                if (!_args.ContainsKey(args[i]))
                {
                    foreach (var o in ProcessCluster(args[i]))
                    {
                        var param = _args[o];
                        _ = ExecuteParam(param, nextArg, ref i);
                    }
                }
                else
                {
                    var param = _args[args[i]];
                    _ = ExecuteParam(param, nextArg, ref i);
                }
            }
        }

        private bool ExecuteParam(Param param, string nextArg, ref int index)
        {
            if (param.HasMoreParams && nextArg == string.Empty)
                return false;

            param.Method(this, nextArg);
            if (param.HasMoreParams)
                index++;

            return true;
        }

        private IEnumerable<string> ProcessCluster(string arg)
        {
            if (arg.StartsWith("-"))
            {
                var arr = arg.ToCharArray();
                for (var j = 1; j < arr.Length; j++)
                {
                    if (arr[j] is '-') break;

                    foreach (var a in _args)
                    {
                        if (a.Key.Equals($"-{arr[j]}"))
                            yield return a.Key;
                    }
                }
            }
        }

        public override string ToString()
        {
            return $"-h {Help} -d {DirNames} -r {Recurse} -v {Verbose} -t {TestMode} -p {Path?.FullName} --out {PathToSave?.FullName}";
        }
    }
}