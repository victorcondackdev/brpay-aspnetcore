using BrPay.AspNetCore.Internal;

namespace BrPay.AspNetCore.Tests;

public sealed class DocumentRulesTests
{
    [Theory]
    [InlineData("529.982.247-25")]
    [InlineData("52998224725")]
    [InlineData("111.444.777-35")]
    public void Cpf_valid(string input)
    {
        Assert.Equal(DocumentError.None, DocumentRules.ValidateCleanCpf(DocumentRules.Clean(input)));
    }

    [Theory]
    [InlineData("529.982.247-24", "CheckDigit")]
    [InlineData("111.111.111-11", "Repeated")]
    [InlineData("1234567890", "Length")]
    [InlineData("5299822472A", "Characters")]
    public void Cpf_invalid(string input, string expected)
    {
        // DocumentError is internal, so the expectation travels as its name.
        Assert.Equal(Enum.Parse<DocumentError>(expected), DocumentRules.ValidateCleanCpf(DocumentRules.Clean(input)));
    }

    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Cnpj_numeric_valid(string input)
    {
        Assert.True(DocumentRules.IsValidCnpj(input));
        Assert.True(DocumentRules.IsValidCnpj(input, allowAlphanumeric: false));
    }

    [Fact]
    public void Cnpj_numeric_check_digit_mismatch()
    {
        Assert.Equal(DocumentError.CheckDigit, DocumentRules.ValidateCleanCnpj(DocumentRules.Clean("11.222.333/0001-80")));
    }

    // Example published by the Receita Federal for the alphanumeric CNPJ.
    [Theory]
    [InlineData("12.ABC.345/01DE-35")]
    [InlineData("12.abc.345/01de-35")]
    [InlineData("12ABC34501DE35")]
    public void Cnpj_alphanumeric_valid(string input)
    {
        Assert.True(DocumentRules.IsValidCnpj(input));
        Assert.False(DocumentRules.IsValidCnpj(input, allowAlphanumeric: false));
    }

    [Fact]
    public void Cnpj_letter_in_check_digit_is_rejected()
    {
        Assert.Equal(DocumentError.Characters, DocumentRules.ValidateCleanCnpj("12ABC34501DE3A"));
    }

    [Fact]
    public void Format_and_mask()
    {
        Assert.Equal("529.982.247-25", DocumentRules.FormatCpf("52998224725"));
        Assert.Equal("***.982.247-**", DocumentRules.MaskCpf("52998224725"));
        Assert.Equal("12.ABC.345/01DE-35", DocumentRules.FormatCnpj(DocumentRules.Clean("12abc34501de35")));
        Assert.Equal("11.***.***/0001-**", DocumentRules.MaskCnpj("11222333000181"));
    }

    [Fact]
    public void Clean_keeps_unknown_characters_so_validation_rejects_them()
    {
        Assert.Equal("529982247\t25", DocumentRules.Clean("529.982.247\t25"));
        Assert.False(DocumentRules.IsValidCpf("529.982.247\t25"));
    }
}
