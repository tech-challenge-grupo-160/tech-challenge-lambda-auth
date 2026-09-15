namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Domain.Models;

public sealed record ClienteAutenticado(
    int Id,
    string Documento,
    string TipoDocumento,
    string Nome,
    string Role,
    string Status);
