using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.Application.Security;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void Gerar_DeveEmitirTokenComClaimsDoCliente()
    {
        var generator = new JwtTokenGenerator(new JwtOptions
        {
            Issuer = "issuer-test",
            Audience = "audience-test",
            SecretKey = "apenas-execucao-local-nao-e-chave-real-256bits",
            ExpirationMinutes = 60
        });
        var cliente = new ClienteAutenticado(
            1000,
            "47654866801",
            "CPF",
            "Vanessa Luna Duarte",
            "Cliente",
            "Ativo");

        var result = generator.Gerar(cliente);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal("issuer-test", token.Issuer);
        Assert.Contains("audience-test", token.Audiences);
        Assert.True(result.ExpiraEm > DateTime.UtcNow);
        Assert.Equal("1000", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("47654866801", token.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("Vanessa Luna Duarte", token.Claims.Single(claim => claim.Type == ClaimTypes.Name).Value);
        Assert.Equal("Cliente", token.Claims.Single(claim => claim.Type == ClaimTypes.Role).Value);
        Assert.Equal("47654866801", token.Claims.Single(claim => claim.Type == "documento").Value);
        Assert.Equal("CPF", token.Claims.Single(claim => claim.Type == "tipo_documento").Value);
        Assert.Equal("Ativo", token.Claims.Single(claim => claim.Type == "status").Value);
    }
}
