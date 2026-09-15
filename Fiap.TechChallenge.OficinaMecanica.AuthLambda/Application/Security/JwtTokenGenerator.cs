using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;
using Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;
using Microsoft.IdentityModel.Tokens;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Security;

public sealed class JwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(JwtOptions jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }

    public TokenResult Gerar(ClienteAutenticado cliente)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, cliente.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, cliente.Documento),
            new(ClaimTypes.Name, cliente.Nome),
            new(ClaimTypes.Role, cliente.Role),
            new("documento", cliente.Documento),
            new("tipo_documento", cliente.TipoDocumento),
            new("status", cliente.Status)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new TokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiraEm = expires
        };
    }
}
