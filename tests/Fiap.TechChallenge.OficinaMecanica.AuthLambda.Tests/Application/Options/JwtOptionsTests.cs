using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Secrets;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.Application.Options;

/// <summary>
/// Cobre a origem da chave de assinatura (issue #46 / F3-12).
///
/// O teste mais importante desta classe e o
/// <see cref="FromEnvironment_DeveFalharQuandoNenhumaOrigemForConfigurada"/>:
/// ate a issue #46 havia um default em codigo, versionado em repositorio
/// publico, e um deploy sem a variavel de ambiente assinaria tokens com ele.
/// </summary>
[Collection(nameof(VariaveisDeAmbienteCollection))]
public sealed class JwtOptionsTests : IDisposable
{
    private static readonly string[] VariaveisUsadas =
    [
        "Jwt__SecretId", "JWT_SECRET_ID",
        "Jwt__SecretKey", "JWT_SECRET_KEY",
        "Jwt__Issuer", "JWT_ISSUER",
        "Jwt__Audience", "JWT_AUDIENCE",
        "Jwt__ExpirationMinutes", "JWT_EXPIRATION_MINUTES"
    ];

    public JwtOptionsTests() => LimparVariaveis();

    public void Dispose() => LimparVariaveis();

    private static void LimparVariaveis()
    {
        foreach (var nome in VariaveisUsadas)
        {
            Environment.SetEnvironmentVariable(nome, null);
        }
    }

    [Fact]
    public void FromEnvironment_DeveFalharQuandoNenhumaOrigemForConfigurada()
    {
        var excecao = Assert.Throws<InvalidOperationException>(
            () => JwtOptions.FromEnvironment(new FakeSecretResolver("nao-deve-ser-chamado")));

        Assert.Contains("JWT_SECRET_ID", excecao.Message);
        Assert.Contains("JWT_SECRET_KEY", excecao.Message);
    }

    [Fact]
    public void FromEnvironment_DeveLerDoSecretsManagerQuandoSecretIdForInformado()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_ID", "tc-grupo160/hom/jwt-signing-key");
        var resolver = new FakeSecretResolver("chave-vinda-do-secrets-manager");

        var options = JwtOptions.FromEnvironment(resolver);

        Assert.Equal("chave-vinda-do-secrets-manager", options.SecretKey);
        Assert.Equal("tc-grupo160/hom/jwt-signing-key", resolver.SecretIdRecebido);
    }

    [Fact]
    public void FromEnvironment_DevePreferirOSecretsManagerQuandoAsDuasOrigensExistirem()
    {
        // Se alguem deixar a chave local para tras, o segredo gerenciado vence.
        Environment.SetEnvironmentVariable("JWT_SECRET_ID", "tc-grupo160/hom/jwt-signing-key");
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "chave-local-esquecida");

        var options = JwtOptions.FromEnvironment(new FakeSecretResolver("chave-gerenciada"));

        Assert.Equal("chave-gerenciada", options.SecretKey);
    }

    [Fact]
    public void FromEnvironment_DeveAceitarChaveLocalQuandoNaoHouverSecretId()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "chave-de-desenvolvimento-local");

        var options = JwtOptions.FromEnvironment(new FakeSecretResolver("nao-deve-ser-chamado"));

        Assert.Equal("chave-de-desenvolvimento-local", options.SecretKey);
    }

    [Fact]
    public void FromEnvironment_DeveManterIssuerAudienceEExpiracaoPadrao()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_KEY", "chave-qualquer");

        var options = JwtOptions.FromEnvironment(new FakeSecretResolver("x"));

        Assert.Equal("Fiap.TechChallenge.OficinaMecanica", options.Issuer);
        Assert.Equal("Fiap.TechChallenge.OficinaMecanica", options.Audience);
        Assert.Equal(60, options.ExpirationMinutes);
    }

    [Fact]
    public void FromEnvironment_DevePropagarFalhaDoResolver()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET_ID", "segredo-inexistente");

        Assert.Throws<InvalidOperationException>(
            () => JwtOptions.FromEnvironment(new ResolverQueFalha()));
    }

    private sealed class FakeSecretResolver(string valor) : ISecretResolver
    {
        public string? SecretIdRecebido { get; private set; }

        public string Resolver(string secretId)
        {
            SecretIdRecebido = secretId;
            return valor;
        }
    }

    private sealed class ResolverQueFalha : ISecretResolver
    {
        public string Resolver(string secretId) =>
            throw new InvalidOperationException($"Falha ao ler o segredo '{secretId}'.");
    }
}

/// <summary>
/// As variaveis de ambiente sao estado global do processo. Sem serializar,
/// classes de teste em paralelo se sobrescrevem e falham de forma intermitente.
/// </summary>
[CollectionDefinition(nameof(VariaveisDeAmbienteCollection), DisableParallelization = true)]
public sealed class VariaveisDeAmbienteCollection;
