namespace FCC;

internal class Size
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

    public enum Format
    {
        Normal, Optimal, LowerOptimal, Smart
    }

    public Size(Kind type = Kind.KiB)
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

    public string ToString(Format format) => format switch
    {
        Format.LowerOptimal => ToString(CalculateOptimalLower(out _)),
        Format.Optimal      => ToString(CalculateOptimal()),
        Format.Normal       => ToString(this.Type),
        Format.Smart        => ToString(),
        _ => "Size[Format:Unknown]"
    };

    private Size(Size size)
    {
        this.Type = size.Type;
        this.Value = size.Value;
    }

    public Size Copy() => new Size(this);

    public static Size FromBytes(long bytes, Kind type = Kind.KiB)
    {
        var size = new Size(type);
        size.AddBytes(bytes);
        return size;
    }

    public static Size operator +(Size s1, Size s2)
    {
        var newSize = new Size(s1);
        newSize.AddBytes(s2.Value * (long)s2.Type);
        return newSize;
    }

    public static Size operator -(Size s1, Size s2)
    {
        var newSize = new Size(s1);
        newSize.RemoveBytes(s2.Value * (long)s2.Type);
        return newSize;
    }
}