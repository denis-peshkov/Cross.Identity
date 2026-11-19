namespace Cross.Identity.Tests.Services;

[TestFixture]
public sealed class UserAccountGuardTests : EFTestsBase
{
    [Test]
    [Category(TestCategory.UNIT)]
    public void GivenInactiveUser_WhenEnsureIsActive_ThenThrowsNotAuthorized()
    {
        var user = new UserAccountEntity
        {
            Id = Guid.NewGuid(),
            UserName = "disabled",
            NormalizedUserName = "disabled",
            IsActive = false,
            CreatedAt = DateTime.UtcNow,
            SecurityStamp = Guid.NewGuid(),
            ConcurrencyStamp = Guid.NewGuid(),
        };

        FluentActions.Invoking(() => UserAccountGuard.EnsureIsActive(user))
            .Should().Throw<NotAuthorizedException>()
            .WithMessage("*Account is disabled*");
    }

    [Test]
    [Category(TestCategory.UNIT)]
    public async Task GivenEmptyUserAccountId_WhenEnsureIsActiveAsync_ThenThrowsNotAuthorizedAsync()
    {
        await FluentActions.Invoking(() => UserAccountGuard.EnsureIsActiveAsync(Context, Guid.Empty, CancellationToken.None))
            .Should().ThrowAsync<NotAuthorizedException>()
            .WithMessage("*Account is disabled*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenOtherVerifiedEmail_WhenEnsureNoOtherVerifiedEmailAsync_ThenThrowsConflictAsync()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        AddToDb(
            new UserAccountEntity
            {
                Id = ownerId,
                Email = "owner@example.com",
                UserName = "owner",
                NormalizedUserName = "owner",
                EmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = otherId,
                Email = "taken@example.com",
                UserName = "other",
                NormalizedUserName = "other",
                EmailVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });

        await FluentActions.Invoking(() =>
                UserAccountGuard.EnsureNoOtherVerifiedEmailAsync(
                    Context,
                    ownerId,
                    "taken@example.com",
                    CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*Email already exists*");
    }

    [Test]
    [Category(TestCategory.INTEGRATION)]
    public async Task GivenOtherVerifiedPhone_WhenEnsureNoOtherVerifiedPhoneAsync_ThenThrowsConflictAsync()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        AddToDb(
            new UserAccountEntity
            {
                Id = ownerId,
                PhoneNumber = "+15550001111",
                UserName = "owner-phone",
                NormalizedUserName = "owner-phone",
                PhoneNumberVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            },
            new UserAccountEntity
            {
                Id = otherId,
                PhoneNumber = "+15550002222",
                UserName = "other-phone",
                NormalizedUserName = "other-phone",
                PhoneNumberVerified = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SecurityStamp = Guid.NewGuid(),
                ConcurrencyStamp = Guid.NewGuid(),
            });

        await FluentActions.Invoking(() =>
                UserAccountGuard.EnsureNoOtherVerifiedPhoneAsync(
                    Context,
                    ownerId,
                    "+15550002222",
                    CancellationToken.None))
            .Should().ThrowAsync<ConflictException>()
            .WithMessage("*PhoneNumber already exists*");
    }
}
