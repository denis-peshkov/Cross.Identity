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
    [Obsolete("Potential security issue here, internal use only")]
    GetUserAccountId,
    ExternalLogin,
    ExternalLoginCallback,
    ExternalLoginUnlink,
    ExternalLoginGetAll,
    ExternalLoginMerge,
    Logout,
    LogoutAll,
    CommunicationEndpointsGetAll,
    CommunicationEndpointSetPreferred,
    ChangeAccountEmail,
}
