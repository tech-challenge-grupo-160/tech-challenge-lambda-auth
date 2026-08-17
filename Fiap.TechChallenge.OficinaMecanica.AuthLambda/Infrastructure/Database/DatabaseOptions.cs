namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Infrastructure.Database;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; init; } = null!;

    public static DatabaseOptions FromEnvironment()
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string nao configurada. Use ConnectionStrings__DefaultConnection ou DATABASE_CONNECTION_STRING.");
        }

        return new DatabaseOptions
        {
            ConnectionString = connectionString
        };
    }
}
