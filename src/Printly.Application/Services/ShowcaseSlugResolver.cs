using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

/// <summary>
/// Endereco da vitrine a partir do nome da empresa: base gerada por <see cref="ShowcaseSlug.Generate"/> e o primeiro
/// sufixo livre (-2, -3...) que nao seja reservado. O slug nao e editavel: acompanha o nome da empresa.
/// </summary>
public static class ShowcaseSlugResolver
{
    private const int MaxAttempts = 1000;

    /// <param name="exceptShowcaseId">Vitrine do proprio usuario: o slug atual dela nao conta como ocupado.</param>
    public static async Task<string> ResolveFreeAsync(
        IShowcaseRepository showcaseRepository,
        string companyName,
        Guid? exceptShowcaseId,
        CancellationToken cancellationToken)
    {
        var baseSlug = ShowcaseSlug.Generate(companyName);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var candidate = ShowcaseSlug.WithSuffix(baseSlug, attempt);

            if (ShowcaseSlug.IsReserved(candidate))
                continue;

            if (!await showcaseRepository.SlugExistsAsync(candidate, exceptShowcaseId, cancellationToken))
                return candidate;
        }

        throw new DomainException(ShowcaseSlug.InUseMessage);
    }
}
