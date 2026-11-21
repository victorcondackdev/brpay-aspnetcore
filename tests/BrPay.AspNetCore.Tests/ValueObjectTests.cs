using System.Globalization;

namespace BrPay.AspNetCore.Tests;

public sealed class ValueObjectTests
{
    [Fact]
    public void Cpf_masks_itself_in_ToString_and_interpolation()
    {
        var cpf = Cpf.Parse("529.982.247-25");

        Assert.Equal("52998224725", cpf.Value);
        Assert.Equal("***.982.247-**", cpf.ToString());
        Assert.Equal("Payer ***.982.247-**", $"Payer {cpf}");
        Assert.Equal("Payer ***.982.247-**", string.Format(CultureInfo.InvariantCulture, "Payer {0}", cpf));
        Assert.Equal("529.982.247-25", cpf.ToString("F", CultureInfo.InvariantCulture));
        Assert.Equal("52998224725", $"{cpf:V}");
    }

    [Fact]
    public void Cpf_equality_ignores_punctuation()
    {
        Assert.Equal(Cpf.Parse("529.982.247-25"), Cpf.Parse("52998224725"));
        Assert.NotEqual(Cpf.Parse("529.982.247-25"), Cpf.Parse("111.444.777-35"));
    }

    [Theory]
    [InlineData("529.982.247-24")]
    [InlineData("")]
    [InlineData("not a cpf")]
    public void Cpf_rejects_invalid_input(string input)
    {
        Assert.False(Cpf.TryParse(input, out _));
        Assert.Throws<FormatException>(() => Cpf.Parse(input));
    }

    [Fact]
    public void Cpf_parses_spans()
    {
        Assert.True(Cpf.TryParse("x52998224725x".AsSpan(1, 11), CultureInfo.InvariantCulture, out var cpf));
        Assert.Equal("52998224725", cpf.Value);
    }

    [Fact]
    public void Default_values_are_empty_and_safe()
    {
        Cpf cpf = default;
        PixKey key = default;

        Assert.True(cpf.IsEmpty);
        Assert.Equal(string.Empty, cpf.Value);
        Assert.Equal(string.Empty, cpf.ToString());
        Assert.True(key.IsEmpty);
        Assert.Equal(PixKeyType.Unknown, key.Type);
    }

    [Fact]
    public void Unsupported_format_throws()
    {
        var cpf = Cpf.Parse("52998224725");
        Assert.Throws<FormatException>(() => cpf.ToString("X", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Cnpj_supports_the_alphanumeric_format()
    {
        var cnpj = Cnpj.Parse("12.abc.345/01de-35");

        Assert.Equal("12ABC34501DE35", cnpj.Value);
        Assert.True(cnpj.IsAlphanumeric);
        Assert.Equal("12.ABC.345/01DE-35", cnpj.Formatted);
        Assert.Equal("12.***.***/01DE-**", cnpj.ToString());
        Assert.False(Cnpj.Parse("11.222.333/0001-81").IsAlphanumeric);
    }

    [Fact]
    public void Parsing_is_generic_over_ISpanParsable()
    {
        Assert.Equal(Cpf.Parse("52998224725"), ParseGeneric<Cpf>("529.982.247-25"));
        Assert.Equal(PixKeyType.Phone, ParseGeneric<PixKey>("+5521987654321").Type);
    }

    private static T ParseGeneric<T>(string s)
        where T : ISpanParsable<T> => T.Parse(s.AsSpan(), CultureInfo.InvariantCulture);
}
