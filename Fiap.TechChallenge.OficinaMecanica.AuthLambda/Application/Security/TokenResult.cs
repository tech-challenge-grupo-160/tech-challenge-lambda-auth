namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;

public sealed class TokenResult
{
    public string Token { get; init; } = null!;
    public DateTime ExpiraEm { get; init; }
}
