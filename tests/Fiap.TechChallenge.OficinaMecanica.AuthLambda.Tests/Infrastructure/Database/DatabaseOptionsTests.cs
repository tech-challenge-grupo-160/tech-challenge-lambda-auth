using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Database;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Secrets;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.Application.Options;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.Infrastructure.Database;

/// <summary>
/// Origem da conexao com o banco (issues #61 e #46).
///
/// Espelha <c>JwtOptionsTests</c>: os dois leem segredo gerenciado e a ordem de
/// precedencia precisa ser a mesma, senao um deploy que configure so metade das
/// variaveis se comporta de forma diferente em cada um.
/// </summary>
[Collection(nameof(VariaveisDeAmbienteCollection))]
public sealed class DatabaseOptionsTests : IDisposable
{
    private const string SegredoValido = """
        {
          "host": "tc-grupo160-dev.abc123.us-east-1.rds.amazonaws.com",
          "port": 5432,
          "database": "oficina_mecanica",
          "username": "oficina_admin",
          "password": "senha-de-teste",
          "connectionString": "Host=tc-grupo160-dev.abc123.us-east-1.rds.amazonaws.com;Port=5432;Database=oficina_mecanica;Username=oficina_admin;Password=senha-de-teste;SSL Mode=Require;Trust Server Certificate=true"
        }
        """;

    private static readonly string[] VariaveisUsadas =
    [
        "Database__SecretId", "DB_SECRET_ID",
        "ConnectionStrings__DefaultConnection", "DATABASE_CONNECTION_STRING"
    ];

    public DatabaseOptionsTests() => LimparVariaveis();

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
            () => DatabaseOptions.FromEnvironment(new FakeSecretResolver("nao-deve-ser-chamado")));

        Assert.Contains("DB_SECRET_ID", excecao.Message);
        Assert.Contains("ConnectionStrings__DefaultConnection", excecao.Message);
    }

    [Fact]
    public void FromEnvironment_DeveMontarAConexaoAPartirDoSegredo()
    {
        Environment.SetEnvironmentVariable("DB_SECRET_ID", "tc-grupo160/dev/banco");
        var resolver = new FakeSecretResolver(SegredoValido);

        var options = DatabaseOptions.FromEnvironment(resolver);

        Assert.Contains("Host=tc-grupo160-dev.abc123.us-east-1.rds.amazonaws.com", options.ConnectionString);
        Assert.Contains("SSL Mode=Require", options.ConnectionString);
        Assert.Equal("tc-grupo160/dev/banco", resolver.SecretIdRecebido);
    }

    [Fact]
    public void FromEnvironment_DevePreferirOSegredoQuandoAsDuasOrigensExistirem()
    {
        Environment.SetEnvironmentVariable("DB_SECRET_ID", "tc-grupo160/dev/banco");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", "Host=localhost");

        var options = DatabaseOptions.FromEnvironment(new FakeSecretResolver(SegredoValido));

        Assert.Contains("rds.amazonaws.com", options.ConnectionString);
    }

    [Fact]
    public void FromEnvironment_DeveAceitarConexaoLocalQuandoNaoHouverSegredo()
    {
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=127.0.0.1;Database=oficina_mecanica");

        var options = DatabaseOptions.FromEnvironment(new FakeSecretResolver("nao-deve-ser-chamado"));

        Assert.Equal("Host=127.0.0.1;Database=oficina_mecanica", options.ConnectionString);
    }

    [Fact]
    public void FromEnvironment_DeveFalharQuandoOSegredoNaoForJson()
    {
        Environment.SetEnvironmentVariable("DB_SECRET_ID", "tc-grupo160/dev/banco");

        var excecao = Assert.Throws<InvalidOperationException>(
            () => DatabaseOptions.FromEnvironment(new FakeSecretResolver("isto nao e json")));

        Assert.Contains("tc-grupo160/dev/banco", excecao.Message);
    }

    [Fact]
    public void FromEnvironment_DeveFalharQuandoFaltarAConnectionStringNoSegredo()
    {
        // Cenario real: alguem edita o segredo a mao e grava so a senha.
        Environment.SetEnvironmentVariable("DB_SECRET_ID", "tc-grupo160/dev/banco");

        var excecao = Assert.Throws<InvalidOperationException>(
            () => DatabaseOptions.FromEnvironment(new FakeSecretResolver("""{"password":"x"}""")));

        Assert.Contains("connectionString", excecao.Message);
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
}
