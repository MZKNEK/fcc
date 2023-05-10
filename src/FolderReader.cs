using System.Text;
using Pastel;

namespace FCC;

internal class FolderReader
{
    private readonly uint _minNameLength;
    private readonly Configuration _flags;
    private readonly DirectoryInfo? _mainFolder;

    private readonly uint _minCountInGroup;
    private readonly uint? _maxCountInGroup;

    private ConsoleColors _colors;

    internal struct ConsoleColors
    {
        public ConsoleColors()
        {
            Q_COLOR = "#D33682";
            D_COLOR = "#586E75";
            F_COLOR = "#2AA198";
            B_COLOR = "#002B36";
        }

        public string Q_COLOR;
        public string D_COLOR;
        public string F_COLOR;
        public string B_COLOR;
    }

    [Flags]
    internal enum Configuration
    {
        None        = 0b0000000,
        Verbose     = 0b0000001,
        Recursive   = 0b0000010,
        DirNames    = 0b0000100,
        Hidden      = 0b0001000,
        GroupSize   = 0b0010000,
        RandomEntry = 0b0100000,
        AvgSize     = 0b1000000
    }

    internal FolderReader(DirectoryInfo? folder, Configuration flags = Configuration.None,
        uint minNameLen = 20, uint? maxCountInGroup = null, uint minCountInGroup = 0)
    {
        _maxCountInGroup = maxCountInGroup;
        _minCountInGroup = minCountInGroup;
        _minNameLength = minNameLen;
        _mainFolder = folder;
        _flags = flags;

        _colors = new();
    }

    internal void SetColors(ConsoleColors colors) => _colors = colors;

    internal struct Stats
    {
        public Stats()
        {
            Files = 0;
            Groups = 0;
            Size = new();
        }

        public int Files;
        public int Groups;
        public BiSize Size;
    }

    internal struct Output
    {
        public Output()
        {
            Result = new();
            Stats = new();
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

    private string Pastelize(string s, string f) => s.Pastel(f).PastelBg(_colors.B_COLOR);

    private void ProcessAndAddName(ref StringBuilder builder, DirectoryInfo? dir, ReadOnlySpan<char> fileName, int? count = null,
        BiSize? size = null, bool avgSize = false) => ProcessAndAddName(ref builder, dir, fileName.ToString(), count, size, avgSize);

    private void ProcessAndAddName(ref StringBuilder builder, DirectoryInfo? dir, string fileName, int? count = null,
        BiSize? size = null, bool avgSize = false)
    {
        builder.Append(Pastelize("'", _colors.Q_COLOR));
        if (dir is not null)
            builder.Append(Pastelize(dir.Name, _colors.D_COLOR)).Append(Pastelize("/", _colors.Q_COLOR));

        builder.Append(Pastelize(fileName, _colors.F_COLOR)).Append(Pastelize("'", _colors.Q_COLOR));
        if (count is not null)
            builder.Append(Pastelize($" x{count}", _colors.Q_COLOR));
        if (size is not null)
            builder.Append(Pastelize($" [{(avgSize ? (size / count) : size)}]", _colors.D_COLOR));

        builder.AppendLine();
    }

    private ReadOnlySpan<char> GetCommonName(string name1, string name2)
    {
        var minLength = Math.Min(name1.Length, name2.Length);
        for (int i = 1; i < minLength; i += 3)
        {
            var newLength = minLength - i;
            if (newLength <= _minNameLength)
                break;

            var span1 = name1.AsSpan(0, newLength);
            var span2 = name2.AsSpan(0, newLength);

            if (span1.SequenceEqual(span2))
                return name1.AsSpan(0, newLength - 2).TrimEnd();
        }
        return ReadOnlySpan<char>.Empty;
    }

    private FileInfo[] GetFilesFromDir(DirectoryInfo dir)
    {
        if (_flags.HasFlag(Configuration.Hidden))
            return dir.GetFiles();

        return dir.GetFiles().Where(file => !file.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
    }

    private void ProcessDir(DirectoryInfo dir, ref Output o)
    {
        var addSize = _flags.HasFlag(Configuration.GroupSize);
        var avgSize = addSize && _flags.HasFlag(Configuration.AvgSize);
        var addDirName = dir != _mainFolder && _flags.HasFlag(Configuration.DirNames);
        var files = GetFilesFromDir(dir);

        if (files is null || files.Length < 1)
            return;

        if (_flags.HasFlag(Configuration.Verbose))
        {
            o.Stats.Files += files.Length;
            foreach (var file in files)
            {
                o.Stats.Size.AddBytes(file.Length);
                ProcessAndAddName(ref o.Result, addDirName ? dir : null, file.Name, null,
                    addSize ? BiSize.FromBytes(file.Length) : null);
            }
            return;
        }

        GroupFilesByCommonNamePrefix(files, addDirName, addSize, avgSize, ref o);
    }

    private void GroupFilesByCommonNamePrefix(ReadOnlySpan<FileInfo> files, bool addDirName,
        bool addSize, bool avgSize, ref Output o)
    {
        var nameToAdd = ReadOnlySpan<char>.Empty;
        var size = BiSize.FromBytes(0);

        for (int i = 0, inGroupCnt = 0; i < files.Length; i++)
        {
            if (!nameToAdd.IsEmpty)
            {
                bool lastElement = false;
                if (files[i].Name.AsSpan().StartsWith(nameToAdd))
                {
                    inGroupCnt++;
                    size.AddBytes(files[i].Length);
                    if (i + 1 < files.Length)
                        continue;

                    lastElement = true;
                }

                AddEntry(inGroupCnt, avgSize, addSize, nameToAdd, addDirName ? files[i].Directory : null, size, ref o);

                if (lastElement)
                    continue;
            }

            inGroupCnt = 1;
            size = BiSize.FromBytes(files[i].Length);
            nameToAdd = ReadOnlySpan<char>.Empty;

            if (files.Length > i + 1)
                nameToAdd = GetCommonName(files[i].Name, files[i + 1].Name);

            if (nameToAdd.IsEmpty)
            {
                inGroupCnt = 0;
                AddEntry(1, avgSize, addSize, files[i].Name, addDirName ? files[i].Directory : null, size, ref o);
            }
        }
    }

    private void AddEntry(int inGroupCnt, bool avgSize, bool addSize,
        ReadOnlySpan<char> nameToAdd, DirectoryInfo? dir, BiSize size, ref Output o)
    {
        if (ShouldAddGroup(inGroupCnt))
        {
            o.Stats.Groups++;
            o.Stats.Files += inGroupCnt;
            o.Stats.Size.AddBytes(size.ToBytes());
            ProcessAndAddName(ref o.Result, dir, nameToAdd, inGroupCnt, addSize ? size : null, avgSize);
        }
    }

    private bool ShouldAddGroup(int inGroupCnt)
    {
        if (_maxCountInGroup is null)
            return _minCountInGroup <= inGroupCnt;

        return _maxCountInGroup >= inGroupCnt && _minCountInGroup <= inGroupCnt;
    }

    internal Output Analyze()
    {
        var o = new Output();
        foreach (var dir in GetDirectories(_mainFolder))
            ProcessDir(dir, ref o);

        if (_flags.HasFlag(Configuration.RandomEntry))
        {
            var lines = o.Result.ToString().Split('\n');
            if (lines?.Length > 1)
            {
                o.Result.Clear();
                o.Result.AppendLine(lines[Random.Shared.Next(lines.Length)]);
            }
        }

        if (!_flags.HasFlag(Configuration.Verbose))
        {
            o.Result.AppendLine("-----------------------------")
                .AppendLine($"TOTAL: {o.Stats.Files} FILES | {o.Stats.Groups} GROUPS | {o.Stats.Size}");
        }

        return o;
    }
}