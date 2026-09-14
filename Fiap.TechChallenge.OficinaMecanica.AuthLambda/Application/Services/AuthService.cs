using Amazon.Lambda.Core;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Abstractions;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Contracts.Requests;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Contracts.Responses;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.ValueObjects;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Logging;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Services;

public sealed class AuthService
{
    private const string StatusAtivo = "Ativo";
    private readonly IClienteRepository _clienteRepository;
    private readonly JwtTokenGenerator _tokenGenerator;

    public AuthService(IClienteRepository clienteRepository, JwtTokenGenerator tokenGenerator)
    {
        _clienteRepository = clienteRepository;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<AuthResponse> AutenticarAsync(
        AuthRequest? request,
        ILambdaLogger logger,
        string requestId,
        CancellationToken cancellationToken)
    {
        const string service = nameof(AuthService);
        if (request is null)
        {
            throw new ArgumentException("Contrato de autenticacao e obrigatorio.");
        }

        var documentoInput = request.Documento ?? request.CpfCnpj ?? request.Cpf;
        var documento = Documento.Parse(documentoInput);

        logger.LogInformation(string.Format(
            LogTemplate.Trace,
            service,
            nameof(AutenticarAsync),
            requestId,
            $"Documento recebido {documento.Valor} do tipo {documento.Tipo}"));

        var cliente = await _clienteRepository.ObterPorDocumentoAsync(
            documento.Valor,
            documento.Tipo,
            logger,
            requestId,
            cancellationToken);

        if (cliente is null || !string.Equals(cliente.Status, StatusAtivo, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning(string.Format(
                LogTemplate.Warning,
                service,
                nameof(AutenticarAsync),
                requestId,
                $"Autenticacao negada para documento {documento.Valor}"));
            throw new UnauthorizedAccessException("Cliente nao encontrado ou inativo.");
        }

        logger.LogInformation(string.Format(
            LogTemplate.Trace,
            service,
            nameof(AutenticarAsync),
            requestId,
            $"Gerando token para cliente {cliente.Id} documento {cliente.Documento}"));

        var token = _tokenGenerator.Gerar(cliente);

        logger.LogInformation(string.Format(
            LogTemplate.Trace,
            service,
            nameof(AutenticarAsync),
            requestId,
            $"Token gerado para cliente {cliente.Id} com expiracao {token.ExpiraEm:O}"));

        return new AuthResponse
        {
            Token = token.Token,
            ExpiraEm = token.ExpiraEm,
            NomeUsuario = cliente.Nome,
            Role = cliente.Role
        };
    }
}
