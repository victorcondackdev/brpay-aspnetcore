using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BrPay.AspNetCore.Tests;

/// <summary>Hosts samples/Sample.Api in memory, in the Production environment so that binding
/// failures become plain 400 responses instead of developer exception pages.</summary>
public sealed class SampleApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Production");
}

public sealed class EndToEndTests : IClassFixture<SampleApiFactory>
{
    private readonly SampleApiFactory _factory;

    public EndToEndTests(SampleApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Post_with_valid_payer_returns_201_with_masked_document()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/payers", new
        {
            document = "529.982.247-25",
            name = "Fulana de Tal",
            key = "fulana@example.com",
            amountCents = 15_000,
            postalCode = "20040-020",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/payers/52998224725", response.Headers.Location?.OriginalString);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("***.982.247-**", body.RootElement.GetProperty("document").GetString());
    }

    [Fact]
    public async Task Post_with_invalid_fields_returns_validation_problem()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/payers", new
        {
            document = "529.982.247-24",
            name = "",
            key = "52998224725",
            amountCents = 5,
            postalCode = "123",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(400, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Dados do pagador inválidos.", body.RootElement.GetProperty("title").GetString());

        var errors = body.RootElement.GetProperty("errors");
        string[] expected = ["amountCents", "document", "key", "name", "postalCode"];
        var actual = errors.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected, actual);
        Assert.Equal("O campo Document deve conter um CPF válido.", errors.GetProperty("document")[0].GetString());
    }

    [Theory]
    [InlineData("fulana@example.com", "Email", "f***@example.com")]
    [InlineData("%2B5521987654321", "Phone", "+5521*****4321")]
    [InlineData("529.982.247-25", "Cpf", "***.982.247-**")]
    public async Task Get_binds_PixKey_from_the_route(string key, string type, string masked)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync("/keys/" + key);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(type, body.RootElement.GetProperty("type").GetString());
        Assert.Equal(masked, body.RootElement.GetProperty("masked").GetString());
    }

    [Fact]
    public async Task Get_with_invalid_route_value_returns_400()
    {
        using var client = _factory.CreateClient();

        using var keyResponse = await client.GetAsync("/keys/not-a-key");
        using var cpfResponse = await client.GetAsync("/payers/52998224724");

        Assert.Equal(HttpStatusCode.BadRequest, keyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, cpfResponse.StatusCode);
    }

    [Fact]
    public async Task Parameter_attributes_are_validated()
    {
        using var client = _factory.CreateClient();

        using var ok = await client.GetAsync("/companies?cnpj=12ABC34501DE35");
        using var bad = await client.GetAsync("/companies?cnpj=12ABC34501DE36");

        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        using var body = JsonDocument.Parse(await bad.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("cnpj", out _));
    }
}
