namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;

/// <summary>
/// Desfecho da validacao de um token pelo <see cref="JwtTokenValidator"/>.
///
/// O <c>Motivo</c> existe para o log do authorizer, nunca para a resposta ao
/// chamador: o gateway devolve apenas 401, sem corpo. Dizer a quem apresentou
/// um token invalido se o problema foi a assinatura, o issuer ou a expiracao
/// entrega informacao util para quem esta tentando forjar um.
/// </summary>
public sealed class TokenValidationResult
{
    private static readonly IReadOnlyDictionary<string, string> SemContexto =
        new Dictionary<string, string>();

    private TokenValidationResult()
    {
    }

    public bool Autorizado { get; private init; }

    /// <summary>Por que o token foi recusado. Nulo quando autorizado.</summary>
    public string? Motivo { get; private init; }

    /// <summary>
    /// Claims repassadas ao backend em <c>$context.authorizer</c>. Vazio quando
    /// recusado.
    /// </summary>
    public IReadOnlyDictionary<string, string> Contexto { get; private init; } = SemContexto;

    public static TokenValidationResult Recusar(string motivo)
    {
        return new TokenValidationResult
        {
            Autorizado = false,
            Motivo = motivo
        };
    }

    public static TokenValidationResult Autorizar(IReadOnlyDictionary<string, string> contexto)
    {
        return new TokenValidationResult
        {
            Autorizado = true,
            Contexto = contexto
        };
    }
}
