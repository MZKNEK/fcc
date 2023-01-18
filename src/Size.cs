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

    public Size(Kind type = Kind.KiB)
    {
        Value = 0;
        Type = type;
    }

    public long Value { get; set; }
    public Kind Type { get; init; }

    public long ToBytes() => Value * (long)Type;
    public void AddBytes(long bytes) => Value += bytes / (long)Type;
    public void RemoveBytes(long bytes) => Value -= bytes / (long)Type;
    public override string ToString() => ToOptimalString();

    private string ToOptimalString()
    {
        var bytes = ToBytes();
        foreach (long val in Enum.GetValues(typeof(Kind)))
        {
            if (val > bytes)
                return ToString((Kind)(val / Kibi));
        }
        return ToString(Kind.PiB);
    }

    public string ToString(Kind type)
    {
        var newType = (long)type;
        var oldType = (long)Type;
        var newValue = Value;

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
        return $"{newValue} {type}";
    }

    private Size(Size size)
    {
        Type = size.Type;
        Value = size.Value;
    }

    public Size Copy() => new Size(this);

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