namespace FCC;

internal static class FolderReaderExtension
{
    internal static FolderReader.Configuration SetVerbose(this FolderReader.Configuration e, bool value)
        => e.Set(FolderReader.Configuration.Verbose, value);

    internal static FolderReader.Configuration SetRecursive(this FolderReader.Configuration e, bool value)
        => e.Set(FolderReader.Configuration.Recursive, value);

    internal static FolderReader.Configuration SetDirNames(this FolderReader.Configuration e, bool value)
        => e.Set(FolderReader.Configuration.DirNames, value);

    internal static FolderReader.Configuration SetHidden(this FolderReader.Configuration e, bool value)
        => e.Set(FolderReader.Configuration.Hidden, value);

    internal static FolderReader.Configuration SetGroupSize(this FolderReader.Configuration e, bool value)
        => e.Set(FolderReader.Configuration.GroupSize, value);

    private static FolderReader.Configuration Set(this FolderReader.Configuration e,
        FolderReader.Configuration flag, bool value)
    {
        if (value)
            e |= flag;
        else
            e &= ~flag;
        return e;
    }
}
