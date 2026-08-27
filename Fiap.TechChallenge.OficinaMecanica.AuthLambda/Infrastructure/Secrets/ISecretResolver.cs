namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Secrets;

/// <summary>
/// Resolve o valor de um segredo a partir do seu identificador.
///
/// A abstracao existe para que <c>JwtOptions</c> nao dependa do SDK da AWS e
/// possa ser testada sem rede.
/// </summary>
public interface ISecretResolver
{
    string Resolver(string secretId);
}
