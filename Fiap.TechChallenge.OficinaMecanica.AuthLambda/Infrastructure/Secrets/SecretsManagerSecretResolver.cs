using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Secrets;

/// <summary>
/// Le o segredo do AWS Secrets Manager.
///
/// A chamada acontece uma vez por container: o <c>Function</c> resolve as
/// opcoes dentro de um <c>Lazy</c> estatico, entao invocacoes seguintes na
/// mesma instancia reaproveitam o valor. Uma rotacao do segredo passa a valer
/// no proximo cold start - comportamento documentado na RFC-0002.
/// </summary>
public sealed class SecretsManagerSecretResolver : ISecretResolver
{
    private readonly IAmazonSecretsManager _client;

    public SecretsManagerSecretResolver()
        : this(new AmazonSecretsManagerClient())
    {
    }

    public SecretsManagerSecretResolver(IAmazonSecretsManager client)
    {
        _client = client;
    }

    public string Resolver(string secretId)
    {
        if (string.IsNullOrWhiteSpace(secretId))
        {
            throw new ArgumentException("Identificador do segredo e obrigatorio.", nameof(secretId));
        }

        GetSecretValueResponse response;
        try
        {
            // A Lambda nao expoe entrypoint assincrono na inicializacao estatica.
            // GetAwaiter().GetResult() aqui e seguro: roda fora do pipeline de
            // requisicao, uma unica vez, antes de qualquer handler.
            response = _client
                .GetSecretValueAsync(new GetSecretValueRequest { SecretId = secretId })
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            // A mensagem nomeia o segredo, nunca o valor.
            throw new InvalidOperationException(
                $"Falha ao ler o segredo '{secretId}' no Secrets Manager.", ex);
        }

        if (string.IsNullOrWhiteSpace(response.SecretString))
        {
            throw new InvalidOperationException(
                $"O segredo '{secretId}' existe mas esta vazio.");
        }

        return response.SecretString;
    }
}
