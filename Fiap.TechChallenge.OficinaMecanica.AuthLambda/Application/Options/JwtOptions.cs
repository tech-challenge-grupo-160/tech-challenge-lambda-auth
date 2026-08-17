namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Application.Options;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "Fiap.TechChallenge.OficinaMecanica";
    public string Audience { get; init; } = "Fiap.TechChallenge.OficinaMecanica";
    public string SecretKey { get; init; } = "local-development-secret-key-32chars";
    public int ExpirationMinutes { get; init; } = 60;

    public static JwtOptions FromEnvironment()
    {
        return new JwtOptions
        {
            Issuer = GetEnvironment("Jwt__Issuer", "JWT_ISSUER", "Fiap.TechChallenge.OficinaMecanica"),
            Audience = GetEnvironment("Jwt__Audience", "JWT_AUDIENCE", "Fiap.TechChallenge.OficinaMecanica"),
            SecretKey = GetEnvironment("Jwt__SecretKey", "JWT_SECRET_KEY", "local-development-secret-key-32chars"),
            ExpirationMinutes = GetExpirationMinutes()
        };
    }

    private static int GetExpirationMinutes()
    {
        var value =
            Environment.GetEnvironmentVariable("Jwt__ExpirationMinutes") ??
            Environment.GetEnvironmentVariable("JWT_EXPIRATION_MINUTES");

        return int.TryParse(value, out var minutes) && minutes > 0 ? minutes : 60;
    }

    private static string GetEnvironment(string primaryName, string fallbackName, string defaultValue)
    {
        var value =
            Environment.GetEnvironmentVariable(primaryName) ??
            Environment.GetEnvironmentVariable(fallbackName);

        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }
}
