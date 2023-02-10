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

    public BiSize(Kind type = Kind.KiB)
    {
        Value = 0;
        Type = type;
    }

    public long Value { get; set; }
    public Kind Type { get; init; }

    public long ToBytes() => Value * (long)Type;
    public void AddBytes(long bytes) => this.Value += bytes / (long)this.Type;
    public void RemoveBytes(long bytes) => this.Value -= bytes / (long)this.Type;

    public override string ToString() => ToSmartString();
    public string ToString(Kind type) => $"{GetValueAs(type)} {type}";

    private Kind CalculateOptimal()
    {
        var bytes = ToBytes();
        foreach (long val in Enum.GetValues(typeof(Kind)))
        {
            if (val > bytes)
                return (Kind)(val / Kibi);
        }
        return Kind.PiB;
    }

    private Kind CalculateOptimalLower(out Kind optimal)
    {
        optimal = CalculateOptimal();
        if (optimal > Kind.Bytes)
            return (Kind)((long)optimal / Kibi);

        return Kind.Bytes;
    }

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

    private string ToSmartString()
    {
        var lower = CalculateOptimalLower(out var optimal);
        decimal value = GetValueAs(lower);
        value /= Kibi;

        return $"{value.ToString("F")} {optimal}";
    }

    private BiSize(BiSize size)
    {
        this.Type = size.Type;
        this.Value = size.Value;
    }

    public BiSize Copy() => new BiSize(this);

    public static BiSize FromBytes(long bytes, Kind type = Kind.KiB)
    {
        var size = new BiSize(type);
        size.AddBytes(bytes);
        return size;
    }

    public static BiSize operator +(BiSize s1, BiSize s2)
    {
        var newSize = new BiSize(s1);
        newSize.AddBytes(s2.ToBytes());
        return newSize;
    }

    public static BiSize operator -(BiSize s1, BiSize s2)
    {
        var newSize = new BiSize(s1);
        newSize.RemoveBytes(s2.ToBytes());
        return newSize;
    }
}