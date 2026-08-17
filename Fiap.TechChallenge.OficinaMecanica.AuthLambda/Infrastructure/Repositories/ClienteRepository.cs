using Amazon.Lambda.Core;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Abstractions;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Database;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Logging;
using Npgsql;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Repositories;

public sealed class ClienteRepository : IClienteRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public ClienteRepository(DatabaseOptions options)
    {
        _dataSource = NpgsqlDataSource.Create(options.ConnectionString);
    }

    public async Task<ClienteAutenticado?> ObterPorDocumentoAsync(
        string documento,
        string tipoDocumento,
        ILambdaLogger logger,
        string requestId,
        CancellationToken cancellationToken)
    {
        const string service = nameof(ClienteRepository);
        logger.LogInformation(string.Format(
            LogTemplate.Trace,
            service,
            nameof(ObterPorDocumentoAsync),
            requestId,
            $"Consultando cliente por documento {documento}"));

        const string sql = """
            SELECT "Id", "CpfCnpj", "Nome", "Status"
            FROM "Cliente"
            WHERE "CpfCnpj" = @documento
            LIMIT 1;
            """;

        await using var command = _dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("documento", documento);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            logger.LogWarning(string.Format(
                LogTemplate.Warning,
                service,
                nameof(ObterPorDocumentoAsync),
                requestId,
                $"Cliente nao encontrado para documento {documento}"));
            return null;
        }

        var cliente = new ClienteAutenticado(
            reader.GetInt32(0),
            reader.GetString(1),
            tipoDocumento,
            reader.GetString(2),
            "Cliente",
            reader.GetString(3));

        logger.LogInformation(string.Format(
            LogTemplate.Trace,
            service,
            nameof(ObterPorDocumentoAsync),
            requestId,
            $"Cliente encontrado para documento {documento} com status {cliente.Status}"));

        return cliente;
    }
}
