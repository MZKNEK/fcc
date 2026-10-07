using System.Globalization;
using FCC;
using Pastel;
using Xunit;

namespace Fcc.Tests;

public sealed class FolderReaderTests : IDisposable
{
    // 25-char common prefix, longer than the default --min (20).
    private const string Prefix = "averylongcommonprefix_000";

    private readonly string _root;
    private readonly CultureInfo _original = CultureInfo.CurrentCulture;

    public FolderReaderTests()
    {
        ConsoleExtensions.Disable();
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

        _root = Path.Combine(Path.GetTempPath(), "fcc-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _original;
        try { Directory.Delete(_root, recursive: true); }
        catch { /* best effort cleanup */ }
    }

    private void Write(string name, int bytes)
        => File.WriteAllBytes(Path.Combine(_root, name), new byte[bytes]);

    private FolderReader Reader(FolderReader.Configuration flags = FolderReader.Configuration.None,
        uint minNameLen = 20, uint? maxCountInGroup = null, uint minCountInGroup = 0)
        => new(new DirectoryInfo(_root), flags, minNameLen, maxCountInGroup, minCountInGroup);

    private void Seed()
    {
        Write(Prefix + "1.txt", 4);
        Write(Prefix + "2.txt", 6);
        Write(Prefix + "3.txt", 4);
        Write("solo.txt", 1);
    }

    [Fact]
    public void GroupsFilesByCommonPrefix()
    {
        Seed();

        var output = Reader().Analyze();

        Assert.Equal(4, output.Stats.Files);
        Assert.Equal(2, output.Stats.Groups);
        Assert.Contains(Prefix, output.Result.ToString());
    }

    [Fact]
    public void MinCountInGroup_FiltersOutSmallGroups()
    {
        Seed();

        var output = Reader(minCountInGroup: 2).Analyze();

        Assert.Equal(3, output.Stats.Files);
        Assert.Equal(1, output.Stats.Groups);
    }

    [Fact]
    public void MaxCountInGroup_FiltersOutBigGroups()
    {
        Seed();

        var output = Reader(maxCountInGroup: 1).Analyze();

        Assert.Equal(1, output.Stats.Files);
        Assert.Equal(1, output.Stats.Groups);
        Assert.Contains("solo.txt", output.Result.ToString());
    }

    [Fact]
    public void MinNameLength_PreventsGrouping()
    {
        Seed();

        // A min length higher than the shared prefix keeps every file separate.
        var output = Reader(minNameLen: 40).Analyze();

        Assert.Equal(4, output.Stats.Files);
        Assert.Equal(4, output.Stats.Groups);
    }

    [Fact]
    public void Verbose_ListsEveryFileAndTotal()
    {
        Seed();

        var output = Reader(FolderReader.Configuration.Verbose).Analyze();
        var text = output.Result.ToString();

        Assert.Equal(4, output.Stats.Files);
        Assert.Contains("solo.txt", text);
        Assert.Contains("TOTAL: 4 FILES", text);
    }

    [Fact]
    public void GroupSizeAverage_KeepsFraction()
    {
        Seed();

        var flags = FolderReader.Configuration.GroupSize | FolderReader.Configuration.AvgSize;
        var output = Reader(flags).Analyze();

        // (4 + 6 + 4) / 3 = 4.67 Bytes
        Assert.Contains("4.67 Bytes", output.Result.ToString());
    }

    [Fact]
    public void GroupSizeWithoutAverage_ShowsRawTotal()
    {
        Seed();

        var output = Reader(FolderReader.Configuration.GroupSize).Analyze();

        // total of the group: 4 + 6 + 4 = 14
        Assert.Contains("14.00 Bytes", output.Result.ToString());
    }

    [Fact]
    public void RandomEntry_ReturnsExactlyOneEntry()
    {
        Seed();

        var output = Reader(FolderReader.Configuration.RandomEntry).Analyze();
        var entries = output.Result.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.StartsWith('\''))
            .ToArray();

        Assert.Single(entries);
    }

    [Fact]
    public void Recursive_IncludesSubdirectories()
    {
        Seed();
        var sub = Directory.CreateDirectory(Path.Combine(_root, "sub"));
        File.WriteAllBytes(Path.Combine(sub.FullName, "nested.txt"), new byte[3]);

        var flat = Reader().Analyze();
        var recursive = Reader(FolderReader.Configuration.Recursive).Analyze();

        Assert.Equal(4, flat.Stats.Files);
        Assert.Equal(5, recursive.Stats.Files);
    }

    [Fact]
    public void HiddenFile_IsExcludedByDefault_AndIncludedWithFlag()
    {
        Seed();
        var hiddenPath = Path.Combine(_root, "hidden_longprefix_00000000.txt");
        Write("visible_longprefix_00000000.txt", 1);
        File.WriteAllBytes(hiddenPath, new byte[1]);
        File.SetAttributes(hiddenPath, FileAttributes.Hidden);

        var without = Reader().Analyze();
        var with = Reader(FolderReader.Configuration.Hidden).Analyze();

        Assert.Equal(5, without.Stats.Files);
        Assert.Equal(6, with.Stats.Files);
    }

    [Fact]
    public void ExplicitlyTargetedHiddenDirectory_IsScanned()
    {
        var hidden = Directory.CreateDirectory(Path.Combine(_root, "hiddentarget"));
        File.WriteAllBytes(Path.Combine(hidden.FullName, "file.txt"), new byte[4]);
        File.SetAttributes(hidden.FullName, FileAttributes.Hidden);

        var output = new FolderReader(new DirectoryInfo(hidden.FullName)).Analyze();

        Assert.Equal(1, output.Stats.Files);
    }

    [Fact]
    public void Recursive_SkipsHiddenSubdirectoryByDefault()
    {
        Seed();
        var hidden = Directory.CreateDirectory(Path.Combine(_root, "hidden_sub"));
        File.WriteAllBytes(Path.Combine(hidden.FullName, "inside.txt"), new byte[2]);
        File.SetAttributes(hidden.FullName, FileAttributes.Hidden);

        var without = Reader(FolderReader.Configuration.Recursive).Analyze();
        var with = Reader(FolderReader.Configuration.Recursive | FolderReader.Configuration.Hidden).Analyze();

        Assert.Equal(4, without.Stats.Files);
        Assert.Equal(5, with.Stats.Files);
    }
}
