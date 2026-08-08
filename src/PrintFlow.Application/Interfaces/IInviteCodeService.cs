using PrintFlow.Application.DTOs.InviteCodes;

namespace PrintFlow.Application.Interfaces;

public interface IInviteCodeService
{
    Task<List<InviteCodeDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<InviteCodeDto> GenerateAsync(GenerateInviteCodeRequest request, Guid? generatedByUserId, CancellationToken cancellationToken = default);
    Task RevokeAsync(Guid id, CancellationToken cancellationToken = default);
    Task PurgeExpiredAsync(CancellationToken cancellationToken = default);
}
