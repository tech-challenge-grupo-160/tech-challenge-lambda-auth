using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Microsoft.IdentityModel.Tokens;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;

/// <summary>
/// Valida o token emitido pelo <see cref="JwtTokenGenerator"/>.
///
/// E a contraparte exata do gerador: mesma chave simetrica, mesmo issuer e
/// mesma audience, todos vindos do mesmo <see cref="JwtOptions"/>. Roda na
/// borda, dentro do Lambda authorizer, antes de o trafego atravessar o VPC
/// Link ate o cluster. Ver RFC-0002 e a issue #43.
///
/// Nao toca no banco de proposito. O authorizer decide sobre o token, nao
/// sobre o cliente: revalidar o status a cada requisicao custaria uma consulta
/// ao RDS por chamada e uma saida de VPC que a funcao nao precisa ter. O preco
/// e a janela de ate 60 minutos em que um cliente desativado ainda passa, que
/// e a mesma janela ja aceita pela API .NET.
/// </summary>
public sealed class JwtTokenValidator
{
    private const string PrefixoBearer = "Bearer ";
    private const string ClaimDocumento = "documento";

    // MapInboundClaims desligado de proposito. Ligado - que e o padrao - o
    // handler reescreve os nomes das claims na leitura: "sub" viraria
    // ClaimTypes.NameIdentifier e "unique_name" viraria ClaimTypes.Name, e a
    // busca por JwtRegisteredClaimNames.Sub logo abaixo devolveria nulo.
    // Desligado, o que se le aqui e exatamente o que esta no payload.
    private static readonly JwtSecurityTokenHandler Handler = new() { MapInboundClaims = false };

    private readonly TokenValidationParameters _parametros;

    public JwtTokenValidator(JwtOptions jwtOptions)
    {
        _parametros = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            // Trava o algoritmo. Sem isso a validacao aceita qualquer algoritmo
            // que a chave configurada consiga verificar, e a decisao passa a
            // depender de um detalhe da biblioteca em vez de ser nossa. O
            // gerador assina em HS256 (RFC-0002); nada mais e aceito.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

            // A API .NET valida o mesmo token com a tolerancia padrao de 5
            // minutos. Aqui ela e zero: a borda ser mais rigorosa que o miolo e
            // seguro, porque o que o gateway recusa nunca chega ao cluster - o
            // contrario e que abriria janela.
            //
            // Isso nao torna a expiracao exata na pratica: o authorizer e
            // cacheado por 300s no gateway, entao um token pode continuar
            // passando por ate cinco minutos depois de expirar, vindo do cache
            // e sem invocar esta funcao. Zerar a tolerancia aqui garante que
            // toda invocacao real seja rigorosa, nao que o cache desapareca.
            ClockSkew = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Valida o valor bruto do header <c>Authorization</c>, incluindo o esquema.
    /// </summary>
    public TokenValidationResult ValidarHeader(string? headerAuthorization)
    {
        if (string.IsNullOrWhiteSpace(headerAuthorization))
        {
            return TokenValidationResult.Recusar("Header Authorization ausente.");
        }

        var valor = headerAuthorization.Trim();

        if (!valor.StartsWith(PrefixoBearer, StringComparison.OrdinalIgnoreCase))
        {
            return TokenValidationResult.Recusar("Header Authorization sem o esquema Bearer.");
        }

        return Validar(valor[PrefixoBearer.Length..]);
    }

    /// <summary>
    /// Valida o token ja sem o esquema <c>Bearer</c>.
    /// </summary>
    public TokenValidationResult Validar(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return TokenValidationResult.Recusar("Token ausente.");
        }

        ClaimsPrincipal principal;
        try
        {
            principal = Handler.ValidateToken(token.Trim(), _parametros, out _);
        }
        catch (SecurityTokenExpiredException)
        {
            return TokenValidationResult.Recusar("Token expirado.");
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return TokenValidationResult.Recusar("Assinatura invalida.");
        }
        catch (SecurityTokenInvalidIssuerException)
        {
            return TokenValidationResult.Recusar("Issuer invalido.");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return TokenValidationResult.Recusar("Audience invalida.");
        }
        catch (SecurityTokenInvalidAlgorithmException)
        {
            return TokenValidationResult.Recusar("Algoritmo de assinatura nao permitido.");
        }
        catch (SecurityTokenException)
        {
            // Cobre o resto da familia, inclusive o token malformado.
            return TokenValidationResult.Recusar("Token invalido.");
        }
        catch (ArgumentException)
        {
            // O handler lanca ArgumentException para entrada que nem chega a
            // parecer um JWT.
            return TokenValidationResult.Recusar("Token malformado.");
        }

        var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var documento = principal.FindFirst(ClaimDocumento)?.Value;

        // O gerador monta a claim de papel com ClaimTypes.Role, e o construtor
        // de JwtSecurityToken nao aplica o mapa de saida - entao o payload
        // carrega a URI longa, nao "role". A forma curta e aceita junto para
        // que o contrato sobreviva a uma eventual troca do gerador para
        // CreateJwtSecurityToken, que aplicaria o mapa.
        var role =
            principal.FindFirst(ClaimTypes.Role)?.Value ??
            principal.FindFirst("role")?.Value;

        if (string.IsNullOrWhiteSpace(sub) ||
            string.IsNullOrWhiteSpace(documento) ||
            string.IsNullOrWhiteSpace(role))
        {
            // Assinatura valida mas conteudo fora do contrato: token emitido
            // por outra versao do gerador, ou por outro sistema que compartilhe
            // a chave. Recusar e mais seguro do que repassar contexto
            // incompleto para o backend.
            return TokenValidationResult.Recusar("Token sem as claims obrigatorias do contrato.");
        }

        // As tres claims que a RFC-0002 define como contexto repassado ao
        // backend. Chegam la em $context.authorizer.
        return TokenValidationResult.Autorizar(new Dictionary<string, string>
        {
            ["sub"] = sub,
            ["documento"] = documento,
            ["role"] = role
        });
    }
}
