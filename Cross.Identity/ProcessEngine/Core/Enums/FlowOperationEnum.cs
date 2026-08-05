namespace Cross.Identity.ProcessEngine.Core.Enums;

/// <summary>
/// Supported flow operations.
/// </summary>
public enum FlowOperationEnum
{
    Register,
    Token,
    RefreshToken,
    RequestCode,
    ResetPassword,
    ForgotPassword,
    GetUserId,
    ExternalLogin,
    ExternalLoginCallback,
    ExternalLoginUnlink,
    Logout,
    LogoutAll,
}
