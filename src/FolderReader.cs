using System.Text;
using Pastel;

namespace FCC
{
    internal class FolderReader
    {
        private const string Q_COLOR = "#D33682";
        private const string D_COLOR = "#586E75";
        private const string F_COLOR = "#2AA198";
        private const string B_COLOR = "#002B36";

        private readonly bool _verbose;
        private readonly bool _recurse;
        private readonly bool _includeSubDirName;
        private readonly DirectoryInfo? _mainFolder;

        internal FolderReader(bool v, bool r, bool d, DirectoryInfo? folder)
        {
            _verbose = v;
            _recurse = r;
            _mainFolder = folder;
            _includeSubDirName = d;
        }

        internal struct Stats
        {
            public int Files;
            public int Groups;
        }

        internal struct Output
        {
            public Output()
            {
                Result = new StringBuilder();
                Stas = new Stats();
            }

            public StringBuilder Result;
            public Stats Stas;
        }

        private IEnumerable<DirectoryInfo> GetDirectories()
        {
            if (_mainFolder is not null)
            {
                yield return _mainFolder;
                if (_recurse)
                {
                    foreach (var dir in _mainFolder.GetDirectories())
                        yield return dir;
                }
            }
        }

        private string Pastelize(string s, string f, string b = B_COLOR) => s.Pastel(f).PastelBg(b);

        private string ProcessName(DirectoryInfo? dir, string fileName, int? count = null)
        {
            var builder = new StringBuilder().Append(Pastelize("'", Q_COLOR));
            if (dir is not null)
                builder.Append(Pastelize(dir.Name, D_COLOR)).Append(Pastelize("/", Q_COLOR));

            builder.Append(Pastelize(fileName, F_COLOR)).Append(Pastelize("'", Q_COLOR));
            if (count is not null)
                builder.Append(Pastelize($" x{count}", Q_COLOR));

            return builder.ToString();
        }

        private void ProcessDir(DirectoryInfo dir, ref Output o)
        {
            int cCnt = 0;
            int lcCnt = 0;
            string cName = "";
            string lcName = "";
            bool matched = false;
            bool addDirName = dir != _mainFolder && _includeSubDirName;

            foreach (var file in dir.GetFiles())
            {
                if (_verbose)
                {
                    o.Stas.Files++;
                    o.Result.AppendLine(ProcessName(addDirName ? dir : null, file.Name));
                    continue;
                }

                if (cCnt == 0)
                {
                    cCnt++;
                    lcCnt = cCnt;
                    cName = file.Name;
                    continue;
                }

                if (file.Name.Contains(cName))
                {
                    cCnt++;
                    lcCnt = cCnt;
                    continue;
                }

                var ncName = cName;
                bool cont = false;
                do
                {
                    if (ncName.Length < 20 || matched)
                    {
                        o.Result.AppendLine(ProcessName(addDirName ? dir : null, ncName, cCnt));
                        o.Stas.Files += cCnt;
                        matched = false;
                        cName = file.Name;
                        o.Stas.Groups++;
                        cont = true;
                        lcCnt = 1;
                        cCnt = 1;
                        break;
                    }
                    ncName = ncName.Truncate(ncName.Length - 2);
                } while (!file.Name.Contains(ncName));

                if (cont)
                {
                    continue;
                }

                cCnt++;
                lcCnt = cCnt;
                matched = true;
                cName = ncName;
                lcName = cName;
            }

            if (lcName is not "")
            {
                o.Result.AppendLine(ProcessName(addDirName ? dir : null, lcName, lcCnt));
                o.Stas.Files += lcCnt;
                o.Stas.Groups++;
            }
        }

        internal Output Analyze()
        {
            var o = new Output();
            foreach (var dir in GetDirectories())
                ProcessDir(dir, ref o);

            if (!_verbose)
            {
                o.Result.AppendLine("-----------------------------")
                    .AppendLine($"TOTAL: {o.Stas.Files} FILES | {o.Stas.Groups} GROUPS");
            }

            return o;
        }
    }
}