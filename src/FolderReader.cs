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

        private readonly Configuration _flags;
        private readonly DirectoryInfo? _mainFolder;

        [Flags]
        internal enum Configuration
        {
            None        = 0b0000,
            Verbose     = 0b0001,
            Recursive   = 0b0010,
            DirNames    = 0b0100,
            Hidden      = 0b1000,
        }

        internal FolderReader(DirectoryInfo? folder, Configuration flags = Configuration.None)
        {
            _mainFolder = folder;
            _flags = flags;
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

        private bool IgnoreDir(DirectoryInfo dir)
        {
            var isRoot = dir.Root.Name.Equals(dir.Name);
            var isSystem = dir.Attributes.HasFlag(FileAttributes.System);
            var isHidden = dir.Attributes.HasFlag(FileAttributes.Hidden);
            if (!isRoot && isSystem)
                return true;

            return !isRoot && isHidden && !_flags.HasFlag(Configuration.Hidden);
        }

        private IEnumerable<DirectoryInfo> GetDirectories(DirectoryInfo? dir)
        {
            if (dir is null)
                yield break;

            if (IgnoreDir(dir))
                yield break;

            yield return dir;

            if (_flags.HasFlag(Configuration.Recursive))
            {
                foreach (var d in dir.GetDirectories())
                    foreach (var rd in GetDirectories(d))
                        yield return rd;
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
                var newLength = minLength - i;
                if (newLength <= min)
                    break;

                var span1 = name1.AsSpan(0, newLength);
                var span2 = name2.AsSpan(0, newLength);

                if (span1.SequenceEqual(span2))
                    return name1.AsSpan(0, newLength - 2);
            }
            return ReadOnlySpan<char>.Empty;
        }

        private void ProcessDir(DirectoryInfo dir, ref Output o)
        {
            var addDirName = dir != _mainFolder && _flags.HasFlag(Configuration.DirNames);
            var files = dir.GetFiles();

            o.Stats.Files += files.Length;
            if (_flags.HasFlag(Configuration.Verbose))
            {
                o.Result.AppendJoin('\n', files.Select(file => ProcessName(addDirName ? dir : null, file.Name)));
                o.Result.AppendLine();
                return;
            }

            GroupFilesByCommonNamePrefix(files, addDirName, ref o);
        }

        private void GroupFilesByCommonNamePrefix(ReadOnlySpan<FileInfo> files, bool addDirName, ref Output o)
        {
            var nameToAdd = ReadOnlySpan<char>.Empty;
            for (int i = 0, inGroupCnt = 0; i < files.Length; i++)
            {
                if (!nameToAdd.IsEmpty)
                {
                    bool lastElement = false;
                    if (files[i].Name.AsSpan().StartsWith(nameToAdd))
                    {
                        inGroupCnt++;
                        if (i + 1 < files.Length)
                            continue;

                        lastElement = true;
                    }

                    o.Stats.Groups++;
                    o.Result.AppendLine(ProcessName(addDirName ? files[i].Directory : null, nameToAdd, inGroupCnt));
                    if (lastElement)
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
                    o.Result.AppendLine(ProcessName(addDirName ? files[i].Directory : null, files[i].Name, 1));
                }
            }
        }

        internal Output Analyze()
        {
            var o = new Output();
            foreach (var dir in GetDirectories(_mainFolder))
                ProcessDir(dir, ref o);

            if (!_flags.HasFlag(Configuration.Verbose))
            {
                o.Result.AppendLine("-----------------------------")
                    .AppendLine($"TOTAL: {o.Stats.Files} FILES | {o.Stats.Groups} GROUPS");
            }

            return o;
        }
    }
}