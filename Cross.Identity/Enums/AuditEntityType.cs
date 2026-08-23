namespace Cross.Identity.Enums;

/// <summary>
/// Entity kinds referenced by polymorphic Id fields (e.g. <see cref="Entities.AuditEntity.EntityType"/>).
/// </summary>
public enum AuditEntityType : short
{
    UserAccount = 1,
    AccessToken = 2,
    RefreshToken = 3,
    EmailVerification = 4,
    PhoneVerification = 5,
    UserExternalLogin = 6,
    UserCommunicationEndpoint = 7,
    ExternalLoginState = 8,
    LinkedMessenger = 9,
}
