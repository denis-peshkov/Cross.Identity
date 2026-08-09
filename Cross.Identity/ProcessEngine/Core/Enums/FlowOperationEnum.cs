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
    GetUserId,
    ExternalLogin,
    ExternalLoginCallback,
    ExternalLoginUnlink,
    ExternalLoginGetAll,
    Logout,
    LogoutAll,
    CommunicationEndpointsGetAll,
    CommunicationEndpointSetPreferred,
}
