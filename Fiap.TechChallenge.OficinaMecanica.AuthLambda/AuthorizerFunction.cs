using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Logging;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda;

/// <summary>
/// Lambda authorizer do API Gateway (issue #43 / F3-09).
///
/// Segunda funcao publicada a partir deste mesmo projeto: o artefato e o
/// mesmo, muda so o handler. Compartilhar o assembly com a Lambda de
/// autenticacao e deliberado - as duas leem a mesma chave, do mesmo segredo,
/// pelo mesmo <see cref="JwtOptions"/>. Separar em outro projeto duplicaria a
/// resolucao do segredo, que e justamente a parte onde uma divergencia entre
/// as duas passaria despercebida ate virar 401 em producao.
///
/// Formato de payload 2.0 com resposta simples (<c>isAuthorized</c>), como
/// decidido na RFC-0002. O authorizer nativo de JWT do HTTP API nao serve
/// aqui: ele exige emissor OIDC com JWKS publico e assinatura assimetrica, e
/// nosso token e HS256 com segredo compartilhado.
///
/// Handler: <c>Fiap.TechChallenge.OficinaMecanica.AuthLambda::Fiap.TechChallenge.OficinaMecanica.AuthLambda.AuthorizerFunction::FunctionHandler</c>
/// </summary>
public class AuthorizerFunction
{
    private const string ServiceName = nameof(AuthorizerFunction);
    private const string HeaderAuthorization = "authorization";

    /// <summary>
    /// Validador compartilhado pelo container. A leitura do segredo acontece
    /// uma vez por instancia da funcao.
    ///
    /// <c>PublicationOnly</c> nao guarda excecao: se o Secrets Manager falhar
    /// de forma transitoria na primeira invocacao, a proxima tenta de novo. No
    /// modo padrao a falha ficaria gravada no <see cref="Lazy{T}"/> e aquele
    /// container devolveria erro ate ser reciclado.
    /// </summary>
    private static readonly Lazy<JwtTokenValidator> Compartilhado = new(
        () => new JwtTokenValidator(JwtOptions.FromEnvironment()),
        LazyThreadSafetyMode.PublicationOnly);

    private readonly JwtTokenValidator? _validator;

    public AuthorizerFunction()
    {
    }

    /// <summary>
    /// Injeta o validador em vez de resolver pelo ambiente. Usado pelos testes,
    /// que nao tem Secrets Manager nem variaveis de ambiente.
    /// </summary>
    public AuthorizerFunction(JwtTokenValidator validator)
    {
        _validator = validator;
    }

    public APIGatewayCustomAuthorizerV2SimpleResponse FunctionHandler(
        APIGatewayCustomAuthorizerV2Request request,
        ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        context.Logger.LogInformation(string.Format(LogTemplate.Start, ServiceName, requestId));

        JwtTokenValidator validator;
        try
        {
            validator = _validator ?? Compartilhado.Value;
        }
        catch (Exception ex)
        {
            // Falha de configuracao - segredo ausente, sem permissao, endpoint
            // do Secrets Manager fora do ar -, nao credencial ruim do chamador.
            // Deixar estourar devolve 500 no gateway, que e a resposta honesta:
            // responder 401 faria parecer problema do token de quem chamou e
            // mandaria o time depurar o lado errado.
            context.Logger.LogError(string.Format(
                LogTemplate.Error,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                LogTemplate.CurrentTraceId(),
                ex));
            throw;
        }

        var rota = request.RouteKey ?? "rota nao informada";
        var resultado = validator.ValidarHeader(ExtrairAuthorization(request));

        if (!resultado.Autorizado)
        {
            // A rota e o motivo entram no log; o token, nunca.
            context.Logger.LogWarning(string.Format(
                LogTemplate.Warning,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                $"Acesso negado em {rota}: {resultado.Motivo}"));

            return new APIGatewayCustomAuthorizerV2SimpleResponse
            {
                IsAuthorized = false
            };
        }

        context.Logger.LogInformation(string.Format(
            LogTemplate.End,
            ServiceName,
            requestId,
            $"Acesso autorizado em {rota} para o sub {resultado.Contexto["sub"]}."));

        return new APIGatewayCustomAuthorizerV2SimpleResponse
        {
            IsAuthorized = true,
            Context = resultado.Contexto.ToDictionary(claim => claim.Key, claim => (object)claim.Value)
        };
    }

    /// <summary>
    /// Le o header <c>Authorization</c> do evento.
    ///
    /// O HTTP API entrega os nomes de header em minusculas, mas o dicionario do
    /// evento e um <c>Dictionary</c> comum, com comparacao sensivel a caixa. A
    /// busca aqui ignora a caixa para nao depender desse detalhe do gateway.
    /// </summary>
    private static string? ExtrairAuthorization(APIGatewayCustomAuthorizerV2Request request)
    {
        if (request.Headers is null)
        {
            return null;
        }

        foreach (var header in request.Headers)
        {
            if (string.Equals(header.Key, HeaderAuthorization, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value;
            }
        }

        return null;
    }
}
