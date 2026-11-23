using Printly.Domain.Entities;

namespace Printly.Domain.Tests;

public class UserOnboardingTests
{
    private static User NewUser() => new("Fulano", "a@b.com", "hash", Guid.NewGuid());

    [Fact]
    public void NewUser_HasNoOnboardingCompletedAt()
    {
        Assert.Null(NewUser().OnboardingCompletedAt);
    }

    [Fact]
    public void CompleteOnboarding_SetsTimestamp()
    {
        var user = NewUser();

        user.CompleteOnboarding();

        Assert.NotNull(user.OnboardingCompletedAt);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public async Task CompleteOnboarding_IsIdempotent()
    {
        var user = NewUser();
        user.CompleteOnboarding();
        var first = user.OnboardingCompletedAt;
        var updatedAt = user.UpdatedAt;
        await Task.Delay(20);

        user.CompleteOnboarding();

        Assert.Equal(first, user.OnboardingCompletedAt);
        Assert.Equal(updatedAt, user.UpdatedAt);
    }
}
