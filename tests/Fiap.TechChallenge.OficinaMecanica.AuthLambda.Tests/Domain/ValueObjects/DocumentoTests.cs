using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.ValueObjects;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.Domain.ValueObjects;

public sealed class DocumentoTests
{
    [Theory]
    [InlineData("476.548.668-01", "47654866801", "CPF")]
    [InlineData("47654866801", "47654866801", "CPF")]
    public void Parse_DeveAceitarCpfValido(string input, string esperado, string tipo)
    {
        var documento = Documento.Parse(input);

        Assert.Equal(esperado, documento.Valor);
        Assert.Equal(tipo, documento.Tipo);
        Assert.True(documento.IsCpf);
        Assert.False(documento.IsCnpj);
    }

    [Theory]
    [InlineData("60.617.051/0001-99", "60617051000199", "CNPJ")]
    [InlineData("60617051000199", "60617051000199", "CNPJ")]
    public void Parse_DeveAceitarCnpjValido(string input, string esperado, string tipo)
    {
        var documento = Documento.Parse(input);

        Assert.Equal(esperado, documento.Valor);
        Assert.Equal(tipo, documento.Tipo);
        Assert.True(documento.IsCnpj);
        Assert.False(documento.IsCpf);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("111.111.111-11")]
    [InlineData("476.548.668-00")]
    public void Parse_DeveRejeitarCpfInvalido(string input)
    {
        Assert.Throws<ArgumentException>(() => Documento.Parse(input));
    }

    [Theory]
    [InlineData("11.111.111/1111-11")]
    [InlineData("60.617.051/0001-00")]
    public void Parse_DeveRejeitarCnpjInvalido(string input)
    {
        Assert.Throws<ArgumentException>(() => Documento.Parse(input));
    }
}
