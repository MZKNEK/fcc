using System.Text;

namespace FCC
{
    internal class Arguments
    {
        private readonly string _help  = new StringBuilder()
                .AppendLine("-h     prints help")
                .AppendLine("-d     prints names of subdir(only with r)")
                .AppendLine("-r     enable recursive mode")
                .AppendLine("-v     enable verbose mode")
                .AppendLine("-p     path to dir")
                .AppendLine("--out  path where save output, dir not file")
                .ToString();

        internal Arguments()
        {
            PathToSave = null;
            DirNames = false;
            Recurse = false;
            Verbose = false;
            Help = true;
            Path = null;
        }

        internal bool Help;
        internal bool Recurse;
        internal bool Verbose;
        internal bool DirNames;
        internal DirectoryInfo? Path;
        internal DirectoryInfo? PathToSave;

        internal string GetHelp() => _help;

        internal void ParseArgs(string[] args)
        {
            for (var i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-r":
                        {
                            Recurse = true;
                        }
                        break;
                    case "-v":
                        {
                            Verbose = true;
                        }
                        break;
                    case "-d":
                        {
                            DirNames = true;
                        }
                        break;
                    case "--out":
                        {
                            if (i + 1 >= args.Length)
                            {
                                continue;
                            }

                            var path = args[++i];
                            if (!Directory.Exists(path))
                            {
                                throw new Exception($"Directory '{path}' don't exist!");
                            }
                            PathToSave = new DirectoryInfo(path);
                        }
                        break;
                    case "-p":
                    case "--path":
                        {
                            if (i + 1 >= args.Length)
                            {
                                continue;
                            }

                            var path = args[++i];
                            if (!Directory.Exists(path))
                            {
                                throw new Exception($"Directory '{path}' don't exist!");
                            }
                            Path = new DirectoryInfo(path);
                            Help = false;
                        }
                        break;
                    default:
                        {
                            if (args[i].StartsWith("-"))
                            {
                                var arr = args[i].ToCharArray();
                                for (var j = 1; j < arr.Length; j++)
                                {
                                    if (arr[j] is '-') break;
                                    if (arr[j] is 'r') Recurse = true;
                                    if (arr[j] is 'v') Verbose = true;
                                    if (arr[j] is 'd') DirNames = true;
                                    if (arr[j] is 'p') goto case "-p";
                                }
                            }
                        }
                        break;
                }
            }
        }
    }
}