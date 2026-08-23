namespace Cross.Identity.ProcessEngine.Core.Enums;

/// <summary>
/// Supported flow operations.
/// </summary>
public enum FlowOperationEnum
{
    Register,
    Token,
    VerifyToken,
    RefreshToken,
    RequestCode,
    ChangePassword,
    ResetPassword,
    ForgotPassword,
    [Obsolete("Potential security issue here")] GetUserId,
    ExternalLogin,
    ExternalLoginCallback,
    ExternalLoginUnlink,
    ExternalLoginGetAll,
    Logout,
    LogoutAll,
    CommunicationEndpointsGetAll,
    CommunicationEndpointSetPreferred,
}
