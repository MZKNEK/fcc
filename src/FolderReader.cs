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

        internal readonly string Summary(bool includeGroups)
            => includeGroups
                ? $"{Files} FILES | {Groups} GROUPS | {Size}"
                : $"{Files} FILES | {Size}";
    }

    internal readonly record struct Entry(string? Directory, string Name, int? Count, string? Size);

    internal struct Output
    {
        public Output()
        {
            Result = new();
            Entries = new();
            Stats = new();
        }

        public StringBuilder Result;
        public List<Entry> Entries;
        public Stats Stats;
    }

    private bool IgnoreDir(DirectoryInfo dir)
    {
        if (dir.Attributes.HasFlag(FileAttributes.System))
            return true;

        return dir.Attributes.HasFlag(FileAttributes.Hidden) && !_flags.HasFlag(Configuration.Hidden);
    }

    private IEnumerable<DirectoryInfo> GetDirectories(DirectoryInfo? dir)
    {
        if (dir is null)
            yield break;

        // The explicitly requested folder is always scanned; hidden/system
        // filtering only applies while descending into subdirectories.
        yield return dir;

        if (_flags.HasFlag(Configuration.Recursive))
        {
            foreach (var d in dir.GetDirectories())
            {
                if (IgnoreDir(d))
                    continue;

                // Skip reparse points (symlinks/junctions) to avoid loops and double counting.
                if (d.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    continue;

                foreach (var rd in GetDirectories(d))
                    yield return rd;
            }
        }
    }

    private string Pastelize(string s, string f) => s.Pastel(f).PastelBg(_colors.B_COLOR);

    private void ProcessAndAddName(ref Output o, DirectoryInfo? dir, ReadOnlySpan<char> fileName, int? count = null,
        BiSize? size = null, bool avgSize = false) => ProcessAndAddName(ref o, dir, fileName.ToString(), count, size, avgSize);

    private void ProcessAndAddName(ref Output o, DirectoryInfo? dir, string fileName, int? count = null,
        BiSize? size = null, bool avgSize = false)
    {
        var sizeText = size is null
            ? null
            : (avgSize ? BiSize.AverageString(size, count ?? 0) : size.ToString());

        var entry = new Entry(dir?.Name, fileName, count, sizeText);
        o.Entries.Add(entry);
        AppendEntryLine(ref o, entry);
    }

    private void AppendEntryLine(ref Output o, Entry entry)
    {
        var builder = o.Result;
        builder.Append(Pastelize("'", _colors.Q_COLOR));
        if (entry.Directory is not null)
            builder.Append(Pastelize(entry.Directory, _colors.D_COLOR)).Append(Pastelize("/", _colors.Q_COLOR));

        builder.Append(Pastelize(entry.Name, _colors.F_COLOR)).Append(Pastelize("'", _colors.Q_COLOR));
        if (entry.Count is not null)
            builder.Append(Pastelize($" x{entry.Count}", _colors.Q_COLOR));
        if (entry.Size is not null)
            builder.Append(Pastelize($" [{entry.Size}]", _colors.D_COLOR));

        builder.AppendLine();
    }

    private ReadOnlySpan<char> GetCommonName(string name1, string name2)
    {
        var maxLength = Math.Min(name1.Length, name2.Length);
        if (maxLength <= _minNameLength)
            return ReadOnlySpan<char>.Empty;

        var length = 0;
        while (length < maxLength && name1[length] == name2[length])
            length++;

        if (length <= _minNameLength)
            return ReadOnlySpan<char>.Empty;

        return name1.AsSpan(0, length).TrimEnd();
    }

    private FileInfo[] GetFilesFromDir(DirectoryInfo dir)
    {
        if (_flags.HasFlag(Configuration.Hidden))
            return dir.GetFiles();

        return dir.EnumerateFiles()
            .Where(file => !file.Attributes.HasFlag(FileAttributes.Hidden))
            .ToArray();
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
                ProcessAndAddName(ref o, addDirName ? dir : null, file.Name, null,
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
            ProcessAndAddName(ref o, dir, nameToAdd, inGroupCnt, addSize ? size : null, avgSize);
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

        if (_flags.HasFlag(Configuration.RandomEntry) && o.Entries.Count > 0)
        {
            var pick = o.Entries[Random.Shared.Next(o.Entries.Count)];
            o.Entries.Clear();
            o.Entries.Add(pick);
            o.Result.Clear();
            AppendEntryLine(ref o, pick);
        }

        if (o.Result.Length > 0)
            o.Result.AppendLine("-----------------------------");

        o.Result.AppendLine($"TOTAL: {o.Stats.Summary(!_flags.HasFlag(Configuration.Verbose))}");

        return o;
    }
}