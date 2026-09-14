namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Contracts.Responses;

public sealed class AuthResponse
{
    public string Token { get; init; } = null!;
    public DateTime ExpiraEm { get; init; }
    public string NomeUsuario { get; init; } = null!;
    public string Role { get; init; } = null!;
}
