using System.Text;
using Printly.Application.DTOs.InviteCodes;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Enums;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class InviteCodeService : IInviteCodeService
{
    private readonly IInviteCodeRepository _inviteCodeRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly Random Random = new();

    public InviteCodeService(IInviteCodeRepository inviteCodeRepository, IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _inviteCodeRepository = inviteCodeRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<List<InviteCodeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var inviteCodes = await _inviteCodeRepository.GetAllAsync(cancellationToken);

        var usedByUserIds = inviteCodes
            .Where(i => i.UsedByUserId.HasValue)
            .Select(i => i.UsedByUserId!.Value)
            .Distinct()
            .ToList();

        var usersById = (await _userRepository.GetByIdsAsync(usedByUserIds, cancellationToken))
            .ToDictionary(u => u.Id, u => u);

        return inviteCodes.Select(inviteCode => ToDto(inviteCode, usersById)).ToList();
    }

    public async Task<InviteCodeDto> GenerateAsync(GenerateInviteCodeRequest request, Guid? generatedByUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.OwnerName))
            throw new DomainException("Nome do titular do convite e obrigatorio.");

        string code;
        var attempts = 0;
        do
        {
            code = BuildCode(request.OwnerName);
            attempts++;
            if (attempts > 10)
                throw new DomainException("Nao foi possivel gerar um codigo de convite unico. Tente novamente.");
        }
        while (await _inviteCodeRepository.GetByCodeAsync(code, cancellationToken) is not null);

        var inviteCode = new InviteCode(code, generatedByUserId);

        await _inviteCodeRepository.AddAsync(inviteCode, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(inviteCode, null);
    }

    public async Task RevokeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var inviteCode = await _inviteCodeRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Codigo de convite nao encontrado.");

        inviteCode.Revoke();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredCodes = await _inviteCodeRepository.GetExpiredUnusedAsync(cancellationToken);

        foreach (var inviteCode in expiredCodes)
            _inviteCodeRepository.Remove(inviteCode);

        if (expiredCodes.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string BuildCode(string ownerName)
    {
        var namePart = RemoveDiacritics(ownerName)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)[0]
            .ToUpperInvariant();

        var numberPart = Random.Next(100000, 999999);

        return $"{namePart}{numberPart}";
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != System.Globalization.UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static InviteCodeDto ToDto(InviteCode inviteCode, IReadOnlyDictionary<Guid, User>? usersById)
    {
        var usedByUser = inviteCode.UsedByUserId.HasValue
            ? usersById?.GetValueOrDefault(inviteCode.UsedByUserId.Value)
            : null;

        var status = inviteCode.Status == InviteCodeStatus.Available && inviteCode.IsExpired
            ? "Expired"
            : inviteCode.Status.ToString();

        return new InviteCodeDto(
            inviteCode.Id,
            inviteCode.Code,
            status,
            inviteCode.CreatedAt,
            inviteCode.ExpiresAt,
            inviteCode.UsedAt,
            inviteCode.UsedByUserId,
            usedByUser?.Name,
            usedByUser?.Email,
            inviteCode.RevokedAt);
    }
}
