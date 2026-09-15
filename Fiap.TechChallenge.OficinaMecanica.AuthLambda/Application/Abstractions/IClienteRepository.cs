using Amazon.Lambda.Core;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Abstractions;

public interface IClienteRepository
{
    Task<ClienteAutenticado?> ObterPorDocumentoAsync(
        string documento,
        string tipoDocumento,
        ILambdaLogger logger,
        string requestId,
        CancellationToken cancellationToken);
}
