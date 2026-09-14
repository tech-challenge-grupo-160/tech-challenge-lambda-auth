using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;
using Microsoft.IdentityModel.Tokens;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.Application.Security;

/// <summary>
/// Os testes emitem os tokens com o proprio <see cref="JwtTokenGenerator"/> em
/// vez de usar strings fixas. E o ponto do arquivo: gerador e validador sao as
/// duas pontas do mesmo contrato, e uma mudanca em um que quebre o outro tem
/// que aparecer aqui - nao em producao, no primeiro 401.
/// </summary>
public sealed class JwtTokenValidatorTests
{
    private const string Chave = "chave-de-teste-com-tamanho-suficiente-256bits";
    private const string Issuer = "issuer-test";
    private const string Audience = "audience-test";

    [Fact]
    public void Validar_DeveAutorizarTokenEmitidoPeloGerador()
    {
        var token = GerarToken();

        var resultado = CriarValidator().Validar(token);

        Assert.True(resultado.Autorizado);
        Assert.Null(resultado.Motivo);
    }

    [Fact]
    public void Validar_DeveRepassarSubDocumentoERoleNoContexto()
    {
        var token = GerarToken();

        var resultado = CriarValidator().Validar(token);

        // O contrato de contexto da RFC-0002: nem mais, nem menos.
        Assert.Equal(3, resultado.Contexto.Count);
        Assert.Equal("1000", resultado.Contexto["sub"]);
        Assert.Equal("47654866801", resultado.Contexto["documento"]);
        Assert.Equal("Cliente", resultado.Contexto["role"]);
    }

    [Fact]
    public void Validar_DeveRecusarTokenExpirado()
    {
        // ExpirationMinutes negativo faz o gerador emitir um token ja vencido.
        var token = GerarToken(expiracaoEmMinutos: -1);

        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Token expirado.", resultado.Motivo);
    }

    [Fact]
    public void Validar_DeveRecusarTokenAssinadoComOutraChave()
    {
        var token = GerarToken(chave: "outra-chave-de-teste-com-256bits-de-tamanho");

        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Assinatura invalida.", resultado.Motivo);
    }

    [Fact]
    public void Validar_DeveRecusarTokenDeOutroIssuer()
    {
        var token = GerarToken(issuer: "emissor-desconhecido");

        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Issuer invalido.", resultado.Motivo);
    }

    [Fact]
    public void Validar_DeveRecusarTokenDeOutraAudience()
    {
        var token = GerarToken(audience: "outra-audiencia");

        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Audience invalida.", resultado.Motivo);
    }

    /// <summary>
    /// O token do administrador vem da API .NET, nao da Lambda, e nao tem a
    /// claim <c>documento</c> - ela so existe no fluxo de autenticacao por CPF.
    ///
    /// Exigir <c>documento</c> trancava o administrador para fora da API
    /// inteira: login pela rota publica devolvia token valido, e toda rota
    /// protegida devolvia 403. A matriz de autorizacao e explicita ao dizer que
    /// as rotas de gestao exigem "JWT valido", nao "JWT de cliente".
    /// </summary>
    [Fact]
    public void Validar_DeveAutorizarTokenDeAdministradorSemDocumento()
    {
        var token = GerarTokenBruto(
            new Claim(JwtRegisteredClaimNames.Sub, "1000"),
            new Claim(JwtRegisteredClaimNames.UniqueName, "admin"),
            new Claim(ClaimTypes.Name, "Administrador"),
            new Claim(ClaimTypes.Role, "Administrador"));

        var resultado = CriarValidator().Validar(token);

        Assert.True(resultado.Autorizado);
        Assert.Equal("1000", resultado.Contexto["sub"]);
        Assert.Equal("Administrador", resultado.Contexto["role"]);
        Assert.False(resultado.Contexto.ContainsKey("documento"));
    }

    [Fact]
    public void Validar_DeveRecusarTokenSemRole()
    {
        var token = GerarTokenBruto(
            new Claim(JwtRegisteredClaimNames.Sub, "1000"),
            new Claim("documento", "47654866801"));

        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Token sem as claims obrigatorias do contrato.", resultado.Motivo);
    }

    [Fact]
    public void Validar_DeveRecusarTokenSemSub()
    {
        var token = GerarTokenBruto(new Claim(ClaimTypes.Role, "Cliente"));

        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Token sem as claims obrigatorias do contrato.", resultado.Motivo);
    }

    [Fact]
    public void Validar_DeveRecusarTokenSemAsClaimsDoContrato()
    {
        // Assinatura, issuer e audience corretos - so o conteudo esta fora do
        // contrato. E o caso de um token emitido por outro sistema que
        // compartilhe a chave.
        var chaveDeAssinatura = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave));
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "1000")],
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: new SigningCredentials(chaveDeAssinatura, SecurityAlgorithms.HmacSha256)));

        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Token sem as claims obrigatorias do contrato.", resultado.Motivo);
    }

    [Theory]
    [InlineData("nao-e-um-jwt")]
    [InlineData("a.b.c")]
    public void Validar_DeveRecusarTokenMalformado(string token)
    {
        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Contains(resultado.Motivo, new[] { "Token malformado.", "Token invalido." });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validar_DeveRecusarTokenVazio(string? token)
    {
        var resultado = CriarValidator().Validar(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Token ausente.", resultado.Motivo);
    }

    [Fact]
    public void ValidarHeader_DeveAceitarEsquemaBearerEmQualquerCaixa()
    {
        var token = GerarToken();

        var resultado = CriarValidator().ValidarHeader($"bEaReR {token}");

        Assert.True(resultado.Autorizado);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ValidarHeader_DeveRecusarHeaderAusente(string? header)
    {
        var resultado = CriarValidator().ValidarHeader(header);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Header Authorization ausente.", resultado.Motivo);
    }

    [Fact]
    public void ValidarHeader_DeveRecusarTokenSemOEsquemaBearer()
    {
        var token = GerarToken();

        var resultado = CriarValidator().ValidarHeader(token);

        Assert.False(resultado.Autorizado);
        Assert.Equal("Header Authorization sem o esquema Bearer.", resultado.Motivo);
    }

    [Fact]
    public void ValidarHeader_DeveRecusarOutroEsquemaDeAutenticacao()
    {
        var resultado = CriarValidator().ValidarHeader("Basic dXN1YXJpbzpzZW5oYQ==");

        Assert.False(resultado.Autorizado);
        Assert.Equal("Header Authorization sem o esquema Bearer.", resultado.Motivo);
    }

    /// <summary>
    /// Documenta os nomes com que as claims viajam no payload.
    ///
    /// O gerador monta a claim de papel com <c>ClaimTypes.Role</c>, e o
    /// construtor de <c>JwtSecurityToken</c> nao aplica o mapa de saida - entao
    /// o que vai no token e a URI longa, nao "role". O validador depende disso;
    /// se algum dia o gerador passar a emitir a forma curta, este teste quebra
    /// e aponta para o lugar certo.
    /// </summary>
    [Fact]
    public void Contrato_PayloadDeveCarregarRoleComoUriLonga()
    {
        var payload = new JwtSecurityTokenHandler().ReadJwtToken(GerarToken());

        Assert.Contains(payload.Claims, claim => claim.Type == ClaimTypes.Role);
        Assert.DoesNotContain(payload.Claims, claim => claim.Type == "role");
        Assert.Contains(payload.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub);
        Assert.Contains(payload.Claims, claim => claim.Type == "documento");
    }

    /// <summary>
    /// Emite um token assinado com a chave correta, mas com as claims que o
    /// teste escolher - para reproduzir formatos que o JwtTokenGenerator nao
    /// produz, como o do administrador, emitido pela API .NET.
    /// </summary>
    private static string GerarTokenBruto(params Claim[] claims)
    {
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Chave));
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: new SigningCredentials(chave, SecurityAlgorithms.HmacSha256)));
    }

    private static JwtTokenValidator CriarValidator()
    {
        return new JwtTokenValidator(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SecretKey = Chave
        });
    }

    private static string GerarToken(
        string? chave = null,
        string? issuer = null,
        string? audience = null,
        int expiracaoEmMinutos = 60)
    {
        var generator = new JwtTokenGenerator(new JwtOptions
        {
            Issuer = issuer ?? Issuer,
            Audience = audience ?? Audience,
            SecretKey = chave ?? Chave,
            ExpirationMinutes = expiracaoEmMinutos
        });

        return generator.Gerar(new ClienteAutenticado(
            1000,
            "47654866801",
            "CPF",
            "Vanessa Luna Duarte",
            "Cliente",
            "Ativo")).Token;
    }
}
