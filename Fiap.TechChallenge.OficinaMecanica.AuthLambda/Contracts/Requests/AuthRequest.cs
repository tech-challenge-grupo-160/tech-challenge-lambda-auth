namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Contracts.Requests;

public sealed class AuthRequest
{
    public string? Documento { get; init; }
    public string? CpfCnpj { get; init; }
    public string? Cpf { get; init; }
}
