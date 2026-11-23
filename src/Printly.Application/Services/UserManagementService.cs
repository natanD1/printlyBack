using Printly.Application.DTOs.Users;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IPlanRepository _planRepository;

    public UserManagementService(IUserRepository userRepository, IPlanRepository planRepository)
    {
        _userRepository = userRepository;
        _planRepository = planRepository;
    }

    public async Task<List<ManagedUserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var planNames = (await _planRepository.GetAllAsync(cancellationToken)).ToDictionary(p => p.Id, p => p.Name);

        return users.Select(u => ToDto(u, planNames.GetValueOrDefault(u.PlanId, string.Empty))).ToList();
    }

    internal static ManagedUserDto ToDto(User user, string planName) =>
        new(user.Id, user.Name, user.Email, user.IsAdmin, user.CreatedAt, user.PlanId, planName);
}
