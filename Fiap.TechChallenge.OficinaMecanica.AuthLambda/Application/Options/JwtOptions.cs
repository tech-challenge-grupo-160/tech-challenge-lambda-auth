using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Secrets;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "Fiap.TechChallenge.OficinaMecanica";
    public string Audience { get; init; } = "Fiap.TechChallenge.OficinaMecanica";
    public required string SecretKey { get; init; }
    public int ExpirationMinutes { get; init; } = 60;

    /// <summary>
    /// Monta as opcoes a partir do ambiente.
    ///
    /// A chave de assinatura tem duas origens, nesta ordem:
    ///
    /// 1. <c>JWT_SECRET_ID</c> - nome do segredo no Secrets Manager. E o caminho
    ///    usado na AWS: a variavel de ambiente carrega o identificador, nunca o
    ///    valor. Ver issue #46 e o Terraform em tech-challenge-infra-k8s.
    /// 2. <c>JWT_SECRET_KEY</c> - a chave em claro. Existe apenas para execucao
    ///    local e testes, e precisa ser informada explicitamente.
    ///
    /// Sem nenhuma das duas a inicializacao falha. Nao ha valor padrao: um
    /// default de desenvolvimento em repositorio publico e uma chave que
    /// qualquer pessoa le, e um deploy que esquecesse a variavel passaria a
    /// assinar tokens com ela sem que ninguem percebesse.
    /// </summary>
    public static JwtOptions FromEnvironment(ISecretResolver? secretResolver = null)
    {
        return new JwtOptions
        {
            Issuer = GetEnvironment("Jwt__Issuer", "JWT_ISSUER", "Fiap.TechChallenge.OficinaMecanica"),
            Audience = GetEnvironment("Jwt__Audience", "JWT_AUDIENCE", "Fiap.TechChallenge.OficinaMecanica"),
            SecretKey = ResolverChave(secretResolver ?? new SecretsManagerSecretResolver()),
            ExpirationMinutes = GetExpirationMinutes()
        };
    }

    private static string ResolverChave(ISecretResolver secretResolver)
    {
        var secretId =
            Environment.GetEnvironmentVariable("Jwt__SecretId") ??
            Environment.GetEnvironmentVariable("JWT_SECRET_ID");

        if (!string.IsNullOrWhiteSpace(secretId))
        {
            return secretResolver.Resolver(secretId);
        }

        var chaveDireta =
            Environment.GetEnvironmentVariable("Jwt__SecretKey") ??
            Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

        if (!string.IsNullOrWhiteSpace(chaveDireta))
        {
            return chaveDireta;
        }

        throw new InvalidOperationException(
            "Chave de assinatura do JWT nao configurada. Defina JWT_SECRET_ID com o nome " +
            "do segredo no Secrets Manager, ou JWT_SECRET_KEY para execucao local.");
    }

    private static int GetExpirationMinutes()
    {
        var value =
            Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes") ??
            Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");

        return int.TryParse(value, out var minutes) && minutes > 0 ? minutes : 60;
    }

    private static string GetEnvironment(string primaryName, string fallbackName, string defaultValue)
    {
        var value =
            Environment.GetEnvironmentVariable(primaryName) ??
            Environment.GetEnvironmentVariable(fallbackName);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
