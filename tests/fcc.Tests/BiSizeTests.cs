using System.Globalization;
using FCC;
using Xunit;

namespace Fcc.Tests;

public class BiSizeTests : IDisposable
{
    private readonly CultureInfo _original = CultureInfo.CurrentCulture;

    public BiSizeTests() => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    public void Dispose() => CultureInfo.CurrentCulture = _original;

    [Fact]
    public void FromBytes_Zero_FormatsAsBytes()
        => Assert.Equal("0.00 Bytes", BiSize.FromBytes(0).ToString());

    [Fact]
    public void FromBytes_One_FormatsAsBytes()
        => Assert.Equal("1.00 Bytes", BiSize.FromBytes(1).ToString());

    [Fact]
    public void FromBytes_JustBelowKibi_StaysInBytes()
        => Assert.Equal("1023.00 Bytes", BiSize.FromBytes(1023).ToString());

    [Fact]
    public void FromBytes_ExactlyKibi_FormatsAsKiB()
        => Assert.Equal("1.00 KiB", BiSize.FromBytes(1024).ToString());

    [Fact]
    public void FromBytes_Mebibyte_FormatsAsMiB()
        => Assert.Equal("1.00 MiB", BiSize.FromBytes(1024L * 1024).ToString());

    [Fact]
    public void FromBytes_Gibibyte_FormatsAsGiB()
        => Assert.Equal("1.00 GiB", BiSize.FromBytes(1024L * 1024 * 1024).ToString());

    [Fact]
    public void AverageString_KeepsFractionalBytes()
        => Assert.Equal("3.50 Bytes", BiSize.AverageString(BiSize.FromBytes(7), 2));

    [Fact]
    public void AverageString_ZeroCount_ReturnsTotal()
        => Assert.Equal("7.00 Bytes", BiSize.AverageString(BiSize.FromBytes(7), 0));

    [Fact]
    public void AddBytes_Accumulates()
    {
        var size = BiSize.FromBytes(100);
        size.AddBytes(24);
        Assert.Equal(124L, size.ToBytes());
    }

    [Fact]
    public void OperatorPlus_AddsValues()
        => Assert.Equal(3L, (BiSize.FromBytes(1) + BiSize.FromBytes(2)).ToBytes());

    [Fact]
    public void GetValueAs_ConvertsUnits()
        => Assert.Equal(2L, BiSize.FromBytes(2048).GetValueAs(BiSize.Kind.KiB));
}
