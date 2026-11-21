using System.Text.Json;

namespace BrPay.AspNetCore.Tests;

public sealed class JsonTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    private sealed record Transfer(Cpf Payer, Cnpj Receiver, PixKey Key, Cpf? Guarantor);

    [Fact]
    public void Round_trips_raw_values()
    {
        const string json = """{"payer":"529.982.247-25","receiver":"12.abc.345/01de-35","key":"+55 (21) 98765-4321","guarantor":null}""";

        var transfer = JsonSerializer.Deserialize<Transfer>(json, Web);

        Assert.NotNull(transfer);
        Assert.Equal("52998224725", transfer.Payer.Value);
        Assert.Equal("12ABC34501DE35", transfer.Receiver.Value);
        Assert.Equal(PixKeyType.Phone, transfer.Key.Type);
        Assert.Null(transfer.Guarantor);

        // The default encoder escapes '+' as +, so compare the parsed JSON, not the raw text.
        var written = JsonSerializer.Serialize(transfer, Web);
        using var doc = JsonDocument.Parse(written);
        var root = doc.RootElement;
        Assert.Equal("52998224725", root.GetProperty("payer").GetString());
        Assert.Equal("12ABC34501DE35", root.GetProperty("receiver").GetString());
        Assert.Equal("+5521987654321", root.GetProperty("key").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("guarantor").ValueKind);

        Assert.Equal(transfer, JsonSerializer.Deserialize<Transfer>(written, Web));
    }

    [Theory]
    [InlineData("""{"payer":"529.982.247-24","receiver":"11222333000181","key":"a@example.com"}""")]
    [InlineData("""{"payer":52998224725,"receiver":"11222333000181","key":"a@example.com"}""")]
    [InlineData("""{"payer":"52998224725","receiver":"11222333000181","key":"not-a-key"}""")]
    public void Invalid_values_throw_JsonException(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Transfer>(json, Web));
    }

    [Fact]
    public void Default_values_are_written_as_null()
    {
        Assert.Equal("null", JsonSerializer.Serialize(default(Cpf), Web));
    }

    [Fact]
    public void PixKeyType_is_written_as_a_string()
    {
        Assert.Equal("\"Email\"", JsonSerializer.Serialize(PixKeyType.Email, Web));
    }
}
