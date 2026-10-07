namespace FCC;

internal class BiSize
{
    private const long Kibi = 1024;

    public enum Kind : long
    {
        Bytes   = 1,
        KiB     = Kibi,
        MiB     = KiB * Kibi,
        GiB     = MiB * Kibi,
        TiB     = GiB * Kibi,
        PiB     = TiB * Kibi,
    }

    public BiSize(Kind type = Kind.Bytes)
    {
        Value = 0;
        Type = type;
    }

    public long Value { get; set; }
    public Kind Type { get; init; }

    public long ToBytes() => this.Value * (long)this.Type;

    public BiSize AddBytes(long bytes)
    {
        this.Value += bytes / (long)this.Type;
        return this;
    }

    public BiSize RemoveBytes(long bytes)
    {
        this.Value -= bytes / (long)this.Type;
        return this;
    }

    public override string ToString() => ToSmartString();
    public string ToString(Kind type) => $"{GetValueAs(type)} {type}";

    public long GetValueAs(Kind type)
    {
        var oldType  = (long)this.Type;
        var newType  = (long)type;
        var newValue = this.Value;

        while (newType != oldType)
        {
            if (newType > oldType)
            {
                oldType *= Kibi;
                newValue /= Kibi;
            }
            else
            {
                oldType /= Kibi;
                newValue *= Kibi;
            }
        }

        return newValue;
    }

    private string ToSmartString() => SmartString(ToBytes());

    /// <summary>
    /// Formats an average (e.g. group size divided by file count) keeping
    /// fractional bytes, so 7 B / 2 shows as "3.50 Bytes" instead of "3.00".
    /// </summary>
    public static string AverageString(BiSize total, long count)
        => count <= 0 ? total.ToString() : SmartString((decimal)total.ToBytes() / count);

    private static string SmartString(decimal bytes)
    {
        var type = Kind.Bytes;
        var value = bytes;

        while (value >= Kibi && type < Kind.PiB)
        {
            value /= Kibi;
            type = (Kind)((long)type * Kibi);
        }

        return $"{value.ToString("F")} {type}";
    }

    private BiSize(BiSize size)
    {
        this.Type = size.Type;
        this.Value = size.Value;
    }

    private BiSize(BiSize size, Kind type)
    {
        this.Type = type;
        this.Value = size.Value;
    }

    public BiSize Copy() => new BiSize(this);
    public BiSize Copy(Kind type) => new BiSize(this, type);

    public static BiSize FromBytes(long bytes, Kind type = Kind.Bytes)
        => new BiSize(type).AddBytes(bytes);

    public static BiSize operator +(BiSize s1, BiSize s2)
        => new BiSize(s1).AddBytes(s2.ToBytes());

    public static BiSize operator -(BiSize s1, BiSize s2)
        => new BiSize(s1).RemoveBytes(s2.ToBytes());

    public static BiSize operator /(BiSize s, long? num)
    {
        if (num.HasValue)
            return BiSize.FromBytes(s.ToBytes() / num.Value);

        return new BiSize(s);
    }
}