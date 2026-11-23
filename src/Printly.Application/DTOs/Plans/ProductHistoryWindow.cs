namespace Printly.Application.DTOs.Plans;

/// <summary>Janela de historico de produtos liberada pelo plano: a partir de <see cref="StartUtc"/> (inclusivo).</summary>
public record ProductHistoryWindow(DateTime StartUtc, int Months);
