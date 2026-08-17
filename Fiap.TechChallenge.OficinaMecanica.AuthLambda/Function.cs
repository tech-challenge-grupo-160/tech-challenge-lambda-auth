using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Services;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Contracts.Requests;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Database;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Repositories;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda;

public class Function
{
    private const string ServiceName = nameof(Function);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<AuthService> AuthService = new(() => new AuthService(
        new ClienteRepository(DatabaseOptions.FromEnvironment()),
        new JwtTokenGenerator(JwtOptions.FromEnvironment())));

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var requestId = context.AwsRequestId;
        context.Logger.LogInformation(string.Format(LogTemplate.Start, ServiceName, requestId));

        if (!string.Equals(request.HttpMethod, "POST", StringComparison.OrdinalIgnoreCase))
        {
            context.Logger.LogWarning(string.Format(
                LogTemplate.Warning,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                $"Metodo nao permitido: {request.HttpMethod}"));
            return Error(HttpStatusCode.MethodNotAllowed, "Metodo nao permitido.");
        }

        if (string.IsNullOrWhiteSpace(request.Body))
        {
            context.Logger.LogWarning(string.Format(
                LogTemplate.Warning,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                "Body da requisicao nao informado"));
            return Error(HttpStatusCode.BadRequest, "Body da requisicao e obrigatorio.");
        }

        AuthRequest? authRequest;
        try
        {
            authRequest = JsonSerializer.Deserialize<AuthRequest>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            context.Logger.LogWarning(string.Format(
                LogTemplate.Warning,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                "JSON invalido recebido na requisicao"));
            return Error(HttpStatusCode.BadRequest, "JSON invalido.");
        }

        try
        {
            var response = await AuthService.Value.AutenticarAsync(
                authRequest,
                context.Logger,
                requestId,
                CancellationToken.None);

            context.Logger.LogInformation(string.Format(
                LogTemplate.End,
                ServiceName,
                requestId,
                "Autenticacao realizada com sucesso."));
            return Json(HttpStatusCode.OK, response);
        }
        catch (ArgumentException ex)
        {
            context.Logger.LogWarning(string.Format(
                LogTemplate.Warning,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                ex.Message));
            return Error(HttpStatusCode.BadRequest, ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Logger.LogWarning(string.Format(
                LogTemplate.Warning,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                ex.Message));
            return Error(HttpStatusCode.Unauthorized, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            context.Logger.LogError(string.Format(
                LogTemplate.Error,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                LogTemplate.CurrentTraceId(),
                ex));
            return Error(HttpStatusCode.InternalServerError, "Erro ao processar autenticacao.");
        }
        catch (Exception ex)
        {
            context.Logger.LogError(string.Format(
                LogTemplate.Error,
                ServiceName,
                nameof(FunctionHandler),
                requestId,
                LogTemplate.CurrentTraceId(),
                ex));
            return Error(HttpStatusCode.InternalServerError, "Erro ao processar autenticacao.");
        }
    }

    private static APIGatewayProxyResponse Json(HttpStatusCode statusCode, object body)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json"
            },
            Body = JsonSerializer.Serialize(body, JsonOptions)
        };
    }

    private static APIGatewayProxyResponse Error(HttpStatusCode statusCode, string message)
    {
        return Json(statusCode, new { message });
    }
}
