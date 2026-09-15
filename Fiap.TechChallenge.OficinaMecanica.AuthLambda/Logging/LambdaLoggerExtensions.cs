using Amazon.Lambda.Core;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Logging;

public static class LambdaLoggerExtensions
{
    public static void LogInformation(this ILambdaLogger logger, string message)
    {
        logger.LogLine(message);
    }

    public static void LogWarning(this ILambdaLogger logger, string message)
    {
        logger.LogLine(message);
    }

    public static void LogError(this ILambdaLogger logger, string message)
    {
        logger.LogLine(message);
    }
}
