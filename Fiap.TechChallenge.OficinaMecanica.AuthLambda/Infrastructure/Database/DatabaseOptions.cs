using System.Text.Json;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Secrets;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public required string ConnectionString { get; init; }

    /// <summary>
    /// Monta as opcoes a partir do ambiente, na mesma ordem de precedencia de
    /// <c>JwtOptions</c> - os dois leem segredo gerenciado e precisam se
    /// comportar igual.
    ///
    /// 1. <c>DB_SECRET_ID</c> - nome do segredo no Secrets Manager. E o caminho
    ///    usado na AWS: a variavel carrega o identificador, nunca a credencial.
    ///    O segredo e criado pelo Terraform em tech-challenge-infra-database
    ///    (issue #61) e ja traz a connection string pronta, com SSL Mode.
    /// 2. <c>ConnectionStrings__DefaultConnection</c> ou
    ///    <c>DATABASE_CONNECTION_STRING</c> - a string em claro, apenas para
    ///    execucao local.
    ///
    /// Sem nenhuma das duas a inicializacao falha.
    /// </summary>
    public static DatabaseOptions FromEnvironment(ISecretResolver? secretResolver = null)
    {
        var secretId =
            Environment.GetEnvironmentVariable("Database__SecretId") ??
            Environment.GetEnvironmentVariable("DB_SECRET_ID");

        if (!string.IsNullOrWhiteSpace(secretId))
        {
            var resolver = secretResolver ?? new SecretsManagerSecretResolver();
            return new DatabaseOptions
            {
                ConnectionString = ExtrairConnectionString(resolver.Resolver(secretId), secretId)
            };
        }

        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Conexao com o banco nao configurada. Defina DB_SECRET_ID com o nome do segredo " +
                "no Secrets Manager, ou ConnectionStrings__DefaultConnection para execucao local.");
        }

        return new DatabaseOptions
        {
            ConnectionString = connectionString
        };
    }

    /// <summary>
    /// O segredo guarda um JSON com host, porta, banco, usuario, senha e a
    /// connection string ja montada. Usamos a montada: quem a construiu foi o
    /// mesmo Terraform que criou a instancia, entao ela ja vem com o SSL Mode
    /// que o parameter group exige.
    /// </summary>
    private static string ExtrairConnectionString(string secretJson, string secretId)
    {
        JsonElement raiz;
        try
        {
            raiz = JsonDocument.Parse(secretJson).RootElement;
        }
        catch (JsonException ex)
        {
            // A mensagem nomeia o segredo, nunca o conteudo.
            throw new InvalidOperationException(
                $"O segredo '{secretId}' nao contem JSON valido.", ex);
        }

        if (!raiz.TryGetProperty("connectionString", out var valor) ||
            string.IsNullOrWhiteSpace(valor.GetString()))
        {
            throw new InvalidOperationException(
                $"O segredo '{secretId}' nao tem a propriedade 'connectionString'.");
        }

        return valor.GetString()!;
    }
}
