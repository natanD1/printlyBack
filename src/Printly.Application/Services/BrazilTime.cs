namespace Printly.Application.Services;

/// <summary>
/// Calendario do usuario (America/Sao_Paulo) para as regras de plano por mes. Se o servidor nao tiver o fuso,
/// cai pra UTC-3 fixo (o Brasil nao tem horario de verao desde 2019, entao da o mesmo resultado).
/// </summary>
public static class BrazilTime
{
    public static TimeZoneInfo Zone { get; } = ResolveZone();

    /// <summary>
    /// Inicio (00:00 em Sao Paulo, devolvido em UTC) do mes atual deslocado por <paramref name="monthOffset"/>:
    /// 0 = mes atual, -1 = anterior, 1 = proximo.
    /// </summary>
    public static DateTime MonthStartUtc(DateTimeOffset now, int monthOffset = 0)
    {
        var local = TimeZoneInfo.ConvertTime(now, Zone);
        var monthStart = new DateTime(local.Year, local.Month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(monthOffset);

        return TimeZoneInfo.ConvertTimeToUtc(monthStart, Zone);
    }

    private static TimeZoneInfo ResolveZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone("UTC-03", TimeSpan.FromHours(-3), "UTC-03", "UTC-03");
        }
    }
}
