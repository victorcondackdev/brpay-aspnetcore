using System.ComponentModel.DataAnnotations;

namespace BrPay.AspNetCore.Tests;

public sealed class AttributeTests
{
    private sealed class Payer
    {
        [Cpf]
        public string? PayerCpf { get; set; }

        [Cnpj]
        public string? ReceiverCnpj { get; set; }

        [Cnpj(AllowAlphanumeric = false)]
        public string? LegacyCnpj { get; set; }

        [CpfOrCnpj]
        public string? Document { get; set; }

        [PixKey(AllowedTypes = PixKeyTypes.Email | PixKeyTypes.Evp)]
        public string? Key { get; set; }

        [Cep]
        public string? PostalCode { get; set; }

        [BrlCents(Min = 100, Max = 500_000)]
        public long AmountCents { get; set; } = 1_000;

        [Display(Name = "CPF do titular")]
        [Cpf(ErrorMessage = "CPF inválido: {0}")]
        public string? HolderCpf { get; set; }
    }

    private static List<ValidationResult> Validate(Payer payer)
    {
        var results = new List<ValidationResult>();
        _ = Validator.TryValidateObject(payer, new ValidationContext(payer), results, validateAllProperties: true);
        return results;
    }

    private static string[] InvalidMembers(Payer payer) =>
        Validate(payer).SelectMany(r => r.MemberNames).OrderBy(n => n, StringComparer.Ordinal).ToArray();

    [Fact]
    public void Valid_model_passes()
    {
        var payer = new Payer
        {
            PayerCpf = "529.982.247-25",
            ReceiverCnpj = "12.ABC.345/01DE-35",
            LegacyCnpj = "11.222.333/0001-81",
            Document = "11222333000181",
            Key = "pagamentos@example.com",
            PostalCode = "20040-020",
            HolderCpf = "52998224725",
        };

        Assert.Empty(Validate(payer));
    }

    [Fact]
    public void Nulls_are_valid_so_that_Required_stays_in_charge()
    {
        Assert.Empty(Validate(new Payer()));
    }

    [Fact]
    public void Each_attribute_rejects_bad_input()
    {
        var payer = new Payer
        {
            PayerCpf = "529.982.247-24",
            ReceiverCnpj = "11.222.333/0001-80",
            LegacyCnpj = "12.ABC.345/01DE-35",
            Document = "123",
            Key = "+5521987654321",
            PostalCode = "2004-0020",
            AmountCents = 99,
            HolderCpf = "000",
        };

        string[] expected = ["AmountCents", "Document", "HolderCpf", "Key", "LegacyCnpj", "PayerCpf", "PostalCode", "ReceiverCnpj"];
        Assert.Equal(expected, InvalidMembers(payer));
    }

    [Fact]
    public void Default_messages_are_pt_BR_and_overridable()
    {
        var results = Validate(new Payer { PayerCpf = "1", HolderCpf = "1" });

        Assert.Contains(results, r => r.ErrorMessage == "O campo PayerCpf deve conter um CPF válido.");
        Assert.Contains(results, r => r.ErrorMessage == "CPF inválido: CPF do titular");
    }

    [Fact]
    public void BrlCents_message_shows_bounds_in_reais()
    {
        var result = Assert.Single(Validate(new Payer { AmountCents = 500_001 }));
        Assert.Equal("O campo AmountCents deve ser um valor em centavos entre R$ 1,00 e R$ 5.000,00.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(0L, "R$ 0,00")]
    [InlineData(1L, "R$ 0,01")]
    [InlineData(123_456L, "R$ 1.234,56")]
    [InlineData(-250L, "R$ -2,50")]
    public void FormatReais_is_culture_independent(long cents, string expected)
    {
        Assert.Equal(expected, BrlCentsAttribute.FormatReais(cents));
    }

    [Fact]
    public void BrlCents_rejects_floating_point_amounts()
    {
        var attribute = new BrlCentsAttribute(1, 100);

        Assert.True(attribute.IsValid(50));
        Assert.True(attribute.IsValid(50L));
        Assert.False(attribute.IsValid(50m));
        Assert.False(attribute.IsValid(50.0));
        Assert.False(attribute.IsValid(101));
    }

    [Theory]
    [InlineData("20040-020", true)]
    [InlineData("20040020", true)]
    [InlineData(" 20040-020 ", true)]
    [InlineData("00000-000", false)]
    [InlineData("2004-0020", false)]
    [InlineData("2004002", false)]
    [InlineData("20040-02A", false)]
    public void Cep_shape(string input, bool valid)
    {
        Assert.Equal(valid, CepAttribute.IsValidCep(input));
    }

    [Fact]
    public void Attributes_accept_value_objects()
    {
        Assert.True(new CpfAttribute().IsValid(Cpf.Parse("52998224725")));
        Assert.False(new CpfAttribute().IsValid(default(Cpf)));
        Assert.False(new CnpjAttribute { AllowAlphanumeric = false }.IsValid(Cnpj.Parse("12ABC34501DE35")));
        Assert.True(new PixKeyAttribute { AllowedTypes = PixKeyTypes.Document }.IsValid(PixKey.Parse("52998224725")));
        Assert.False(new PixKeyAttribute { AllowedTypes = PixKeyTypes.Document }.IsValid(PixKey.Parse("a@example.com")));
        Assert.False(new CpfAttribute().IsValid(42));
    }
}
