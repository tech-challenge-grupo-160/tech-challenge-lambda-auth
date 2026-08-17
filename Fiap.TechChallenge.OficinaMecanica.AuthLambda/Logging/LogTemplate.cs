using System.Diagnostics;

namespace Fiap.TechChallenge.OficinaMecanica.AuthLambda.Logging;

public static class LogTemplate
{
    public const string Start = "[Iniciando] service: {0}, requestId: {1}.";
    public const string End = "[Finalizando] service: {0}, requestId: {1}. | {2}";
    public const string Trace = "[Executando] -> service: {0}, method: {1}, requestId: {2} | {3}.";
    public const string Warning = "[Warning] service: {0}, method: {1}, requestId: {2} | {3}.";
    public const string Error = "[Erro] service: {0}, method: {1}, requestId: {2}, traceId: {3} | {4}.";

    public static string CurrentTraceId()
    {
        return Activity.Current?.TraceId.ToString() ?? "n/a";
    }
}
