using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests;

public sealed class AuthorizerFunctionTests
{
    private const string Chave = "chave-de-teste-com-tamanho-suficiente-256bits";
    private const string Issuer = "issuer-test";
    private const string Audience = "audience-test";

    [Fact]
    public void FunctionHandler_DeveAutorizarRequisicaoComTokenValido()
    {
        var request = CriarRequest($"Bearer {GerarToken()}");

        var resposta = CriarFunction().FunctionHandler(request, new FakeLambdaContext());

        Assert.True(resposta.IsAuthorized);
    }

    [Fact]
    public void FunctionHandler_DeveRepassarContextoDoTokenParaOBackend()
    {
        var request = CriarRequest($"Bearer {GerarToken()}");

        var resposta = CriarFunction().FunctionHandler(request, new FakeLambdaContext());

        Assert.NotNull(resposta.Context);
        Assert.Equal("1000", resposta.Context["sub"]);
        Assert.Equal("47654866801", resposta.Context["documento"]);
        Assert.Equal("Cliente", resposta.Context["role"]);
    }

    [Fact]
    public void FunctionHandler_DeveNegarQuandoOHeaderNaoExiste()
    {
        var request = CriarRequest(header: null);

        var resposta = CriarFunction().FunctionHandler(request, new FakeLambdaContext());

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public void FunctionHandler_DeveNegarQuandoNaoHaHeaderAlgum()
    {
        // O gateway pode entregar o evento sem o dicionario de headers.
        var request = new APIGatewayCustomAuthorizerV2Request
        {
            RouteKey = "GET /api/v1/ordens-servico",
            Headers = null
        };

        var resposta = CriarFunction().FunctionHandler(request, new FakeLambdaContext());

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public void FunctionHandler_DeveNegarQuandoOTokenExpirou()
    {
        var request = CriarRequest($"Bearer {GerarToken(expiracaoEmMinutos: -1)}");

        var resposta = CriarFunction().FunctionHandler(request, new FakeLambdaContext());

        Assert.False(resposta.IsAuthorized);
    }

    [Fact]
    public void FunctionHandler_NaoDeveDevolverContextoQuandoNega()
    {
        // Contexto em resposta negada seria vazamento: o backend nunca deve
        // receber claim de uma requisicao que o gateway vai recusar.
        var request = CriarRequest("Bearer token-invalido");

        var resposta = CriarFunction().FunctionHandler(request, new FakeLambdaContext());

        Assert.False(resposta.IsAuthorized);
        Assert.True(resposta.Context is null || resposta.Context.Count == 0);
    }

    [Fact]
    public void FunctionHandler_DeveEncontrarOHeaderIndependenteDaCaixa()
    {
        // O HTTP API entrega os nomes em minusculas, mas o dicionario do evento
        // e sensivel a caixa. O handler nao pode depender disso.
        var request = new APIGatewayCustomAuthorizerV2Request
        {
            RouteKey = "GET /api/v1/ordens-servico",
            Headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {GerarToken()}"
            }
        };

        var resposta = CriarFunction().FunctionHandler(request, new FakeLambdaContext());

        Assert.True(resposta.IsAuthorized);
    }

    private static AuthorizerFunction CriarFunction()
    {
        return new AuthorizerFunction(new JwtTokenValidator(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SecretKey = Chave
        }));
    }

    private static APIGatewayCustomAuthorizerV2Request CriarRequest(string? header)
    {
        var headers = new Dictionary<string, string>();
        if (header is not null)
        {
            headers["authorization"] = header;
        }

        return new APIGatewayCustomAuthorizerV2Request
        {
            RouteKey = "GET /api/v1/ordens-servico",
            Headers = headers
        };
    }

    private static string GerarToken(int expiracaoEmMinutos = 60)
    {
        var generator = new JwtTokenGenerator(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SecretKey = Chave,
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

    private sealed class FakeLambdaContext : ILambdaContext
    {
        public string AwsRequestId => "request-1";

        public IClientContext ClientContext => null!;

        public string FunctionName => "tc-grupo160-authorizer-test";

        public string FunctionVersion => "$LATEST";

        public ICognitoIdentity Identity => null!;

        public string InvokedFunctionArn => string.Empty;

        public ILambdaLogger Logger { get; } = new FakeLambdaLogger();

        public string LogGroupName => string.Empty;

        public string LogStreamName => string.Empty;

        public int MemoryLimitInMB => 512;

        public TimeSpan RemainingTime => TimeSpan.FromSeconds(30);
    }

    private sealed class FakeLambdaLogger : ILambdaLogger
    {
        public void Log(string message)
        {
        }

        public void LogLine(string message)
        {
        }
    }
}
