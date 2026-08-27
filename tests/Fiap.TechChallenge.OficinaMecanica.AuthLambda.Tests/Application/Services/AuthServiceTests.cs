using Amazon.Lambda.Core;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Abstractions;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Services;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Contracts.Requests;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.Application.Services;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task AutenticarAsync_DeveGerarTokenQuandoClienteEstaAtivo()
    {
        var cliente = new ClienteAutenticado(
            1000,
            "47654866801",
            "CPF",
            "Vanessa Luna Duarte",
            "Cliente",
            "Ativo");
        var repository = new FakeClienteRepository(cliente);
        var service = CriarService(repository);

        var response = await service.AutenticarAsync(
            new AuthRequest { Documento = "476.548.668-01" },
            new FakeLambdaLogger(),
            "request-1",
            CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(response.Token));
        Assert.Equal("Vanessa Luna Duarte", response.NomeUsuario);
        Assert.Equal("Cliente", response.Role);
        Assert.Equal("47654866801", repository.DocumentoRecebido);
        Assert.Equal("CPF", repository.TipoDocumentoRecebido);
    }

    [Fact]
    public async Task AutenticarAsync_DeveNegarQuandoClienteNaoExiste()
    {
        var service = CriarService(new FakeClienteRepository(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AutenticarAsync(
            new AuthRequest { Documento = "476.548.668-01" },
            new FakeLambdaLogger(),
            "request-1",
            CancellationToken.None));
    }

    [Theory]
    [InlineData("Inativo")]
    [InlineData("Bloqueado")]
    public async Task AutenticarAsync_DeveNegarQuandoClienteNaoEstaAtivo(string status)
    {
        var cliente = new ClienteAutenticado(
            1000,
            "47654866801",
            "CPF",
            "Vanessa Luna Duarte",
            "Cliente",
            status);
        var service = CriarService(new FakeClienteRepository(cliente));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.AutenticarAsync(
            new AuthRequest { Documento = "476.548.668-01" },
            new FakeLambdaLogger(),
            "request-1",
            CancellationToken.None));
    }

    [Fact]
    public async Task AutenticarAsync_DeveRejeitarContratoNulo()
    {
        var service = CriarService(new FakeClienteRepository(null));

        await Assert.ThrowsAsync<ArgumentException>(() => service.AutenticarAsync(
            null,
            new FakeLambdaLogger(),
            "request-1",
            CancellationToken.None));
    }

    private static AuthService CriarService(IClienteRepository repository)
    {
        return new AuthService(
            repository,
            new JwtTokenGenerator(new JwtOptions
            {
                SecretKey = "chave-de-teste-com-tamanho-suficiente-256bits"
            }));
    }

    private sealed class FakeClienteRepository : IClienteRepository
    {
        private readonly ClienteAutenticado? _cliente;

        public FakeClienteRepository(ClienteAutenticado? cliente)
        {
            _cliente = cliente;
        }

        public string? DocumentoRecebido { get; private set; }
        public string? TipoDocumentoRecebido { get; private set; }

        public Task<ClienteAutenticado?> ObterPorDocumentoAsync(
            string documento,
            string tipoDocumento,
            ILambdaLogger logger,
            string requestId,
            CancellationToken cancellationToken)
        {
            DocumentoRecebido = documento;
            TipoDocumentoRecebido = tipoDocumento;
            return Task.FromResult(_cliente);
        }
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
