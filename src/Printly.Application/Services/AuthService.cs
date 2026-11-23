using Printly.Application.DTOs.Auth;
using Printly.Application.Interfaces;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Services;

public class AuthService : IAuthService
{
    private const string InvalidSessionMessage = "Sessao invalida ou expirada.";

    private readonly IUserRepository _userRepository;
    private readonly IInviteCodeRepository _inviteCodeRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        IUserRepository userRepository,
        IInviteCodeRepository inviteCodeRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IPlanRepository planRepository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _userRepository = userRepository;
        _inviteCodeRepository = inviteCodeRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        PasswordPolicy.Validate(request.Password);

        var inviteCode = await _inviteCodeRepository.GetByCodeAsync(request.InviteCode, cancellationToken)
            ?? throw new DomainException("Codigo de convite invalido.");

        if (!inviteCode.IsValid)
            throw new DomainException("Codigo de convite expirado, revogado ou ja utilizado.");

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            throw new DomainException("Ja existe um usuario cadastrado com este email.");

        // Novo usuario cai no plano marcado como padrao (a marcacao vale, nao o nome).
        var defaultPlan = await _planRepository.GetDefaultAsync(cancellationToken)
            ?? throw new DomainException("Nenhum plano padrao configurado. Entre em contato com o suporte.");

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = new User(request.Name, request.Email, passwordHash, inviteCode.Id, defaultPlan.Id);

        inviteCode.MarkAsUsed(user.Id);

        await _userRepository.AddAsync(user, cancellationToken);
        var response = await IssueSessionAsync(user, Guid.NewGuid(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new DomainException("Email ou senha invalidos.");

        var response = await IssueSessionAsync(user, Guid.NewGuid(), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return response;
    }

    /// <summary>
    /// Rotaciona o refresh token. Ordem pensada para falhar fechado: o token antigo e revogado de forma
    /// atomica (grava na hora) ANTES de criar o novo; se algo falhar depois, o usuario so precisa logar de novo.
    /// </summary>
    public async Task<AuthResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedException(InvalidSessionMessage);

        var now = Now();
        var stored = await _refreshTokenRepository.GetByHashAsync(_tokenService.HashRefreshToken(refreshToken), cancellationToken)
            ?? throw new UnauthorizedException(InvalidSessionMessage);

        // Token ja rotacionado/revogado sendo reapresentado: possivel roubo, derruba a cadeia inteira.
        if (stored.Revoked)
        {
            await _refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            throw new UnauthorizedException(InvalidSessionMessage);
        }

        if (stored.IsExpired(now))
            throw new UnauthorizedException(InvalidSessionMessage);

        var user = await _userRepository.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null)
        {
            await _refreshTokenRepository.TryRevokeAsync(stored.Id, now, cancellationToken);
            throw new UnauthorizedException(InvalidSessionMessage);
        }

        // Ordem importa: grava o token novo ANTES de revogar o atual. Assim, se duas requisicoes usarem o mesmo
        // token ao mesmo tempo, a perdedora do UPDATE condicional revoga a familia ja contendo os tokens novos
        // das duas (reuso derruba a cadeia inteira, sem deixar um token vivo). Um token novo orfao (crash entre
        // os passos) nunca foi entregue a ninguem, entao nao pode ser usado.
        var response = await IssueSessionAsync(user, stored.FamilyId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!await _refreshTokenRepository.TryRevokeAsync(stored.Id, now, cancellationToken))
        {
            await _refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId, now, cancellationToken);
            throw new UnauthorizedException(InvalidSessionMessage);
        }

        return response;
    }

    /// <summary>
    /// Idempotente: nunca revela se o token existia. Encerra a cadeia inteira (familia), mesmo se o token
    /// apresentado ja foi rotacionado: se um atacante rotacionou um token roubado, o logout da vitima o derruba.
    /// </summary>
    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var stored = await _refreshTokenRepository.GetByHashAsync(_tokenService.HashRefreshToken(refreshToken), cancellationToken);

        if (stored is null)
            return;

        await _refreshTokenRepository.RevokeFamilyAsync(stored.FamilyId, Now(), cancellationToken);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Usuario nao encontrado.");

        return ToDto(user);
    }

    public async Task<UserDto> CompleteOnboardingAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new DomainException("Usuario nao encontrado.");

        user.CompleteOnboarding();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    private DateTime Now() => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>Emite access + refresh. Persiste so o hash do refresh; o chamador faz o SaveChanges.</summary>
    private async Task<AuthResponse> IssueSessionAsync(User user, Guid familyId, CancellationToken cancellationToken)
    {
        var (accessToken, accessExpiresIn) = _tokenService.GenerateAccessToken(user);

        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var lifetime = _tokenService.RefreshTokenLifetime;
        var refreshToken = new RefreshToken(
            user.Id,
            _tokenService.HashRefreshToken(rawRefreshToken),
            familyId,
            Now().Add(lifetime));

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return new AuthResponse(accessToken, accessExpiresIn, rawRefreshToken, (int)lifetime.TotalSeconds, ToDto(user));
    }

    private static UserDto ToDto(User user) => new(user.Id, user.Name, user.Email, user.CreatedAt, user.IsAdmin, user.OnboardingCompletedAt is not null);
}
