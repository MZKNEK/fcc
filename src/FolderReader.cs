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
                Stats = new Stats();
            }

            public StringBuilder Result;
            public Stats Stats;
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

        private string ProcessName(DirectoryInfo? dir, ReadOnlySpan<char> fileName, int? count = null)
            => ProcessName(dir, fileName.ToString(), count);

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

        private ReadOnlySpan<char> GetCommonName(string name1, string name2, int min = 20)
        {
            var minLength = Math.Min(name1.Length, name2.Length);
            for (int i = 1; i < minLength; i += 3)
            {
                if (minLength - i <= min)
                    break;

                var span1 = name1.AsSpan(0, minLength - i);
                var span2 = name2.AsSpan(0, minLength - i);
                if (span1.SequenceEqual(span2))
                    return span1;
            }
            return ReadOnlySpan<char>.Empty;
        }

        private void ProcessDir(DirectoryInfo dir, ref Output o)
        {
            var addDirName = dir != _mainFolder && _includeSubDirName;
            var files = dir.GetFiles();

            o.Stats.Files += files.Length;
            if (_verbose)
            {
                o.Result.AppendJoin('\n', files.Select(file => ProcessName(addDirName ? dir : null, file.Name)));
                o.Result.AppendLine();
                return;
            }

            var nameToAdd = ReadOnlySpan<char>.Empty;
            for (int i = 0, inGroupCnt = 0; i < files.Length; i++)
            {
                if (nameToAdd.IsEmpty && files.Length > i + 1)
                    nameToAdd = GetCommonName(files[i].Name, files[i + 1].Name);

                if (nameToAdd.IsEmpty)
                {
                    inGroupCnt = 0;
                    o.Stats.Groups++;
                    o.Result.AppendLine(ProcessName(addDirName ? dir : null, files[i].Name, 1));
                    continue;
                }

                bool lastAlreadyAdded = false;
                if (files[i].Name.AsSpan().StartsWith(nameToAdd))
                {
                    inGroupCnt++;
                    if (i + 1 < files.Length)
                        continue;

                    lastAlreadyAdded = true;
                }

                if (!nameToAdd.IsEmpty)
                {
                    o.Stats.Groups++;
                    o.Result.AppendLine(ProcessName(addDirName ? dir : null, nameToAdd, inGroupCnt));
                    if (lastAlreadyAdded)
                        continue;
                }

                inGroupCnt = 1;
                nameToAdd = ReadOnlySpan<char>.Empty;

                if (files.Length > i + 1)
                    nameToAdd = GetCommonName(files[i].Name, files[i + 1].Name);

                if (nameToAdd.IsEmpty)
                {
                    inGroupCnt = 0;
                    o.Stats.Groups++;
                    o.Result.AppendLine(ProcessName(addDirName ? dir : null, files[i].Name, 1));
                }
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
                    .AppendLine($"TOTAL: {o.Stats.Files} FILES | {o.Stats.Groups} GROUPS");
            }

            return o;
        }
    }
}