using Printly.Application.DTOs.Users;

namespace Printly.Application.Interfaces;

/// <summary>Consulta de usuarios para o admin. A troca de plano fica em IPlanService.ChangeUserPlanAsync.</summary>
public interface IUserManagementService
{
    Task<List<ManagedUserDto>> GetAllAsync(CancellationToken cancellationToken = default);
}
