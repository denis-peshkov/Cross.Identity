namespace Cross.Identity.ProcessEngine.Core.Enums;

/// <summary>
/// Supported flow operations.
/// Explicit values keep stable ordinals across releases (do not renumber existing members).
/// </summary>
public enum FlowOperationEnum
{
    Register = 0,
    Token = 1,
    VerifyToken = 2,
    RefreshToken = 3,
    RequestCode = 4,
    ChangePassword = 5,
    ResetPassword = 6,
    ForgotPassword = 7,
    [Obsolete("Potential security issue here, internal use only")]
    GetUserAccountId = 8,
    ExternalLogin = 9,
    ExternalLoginGetAll = 12,
    ExternalLoginCallback = 10,
    ExternalLoginUnlink = 11,
    ExternalLoginMerge = 18,
    Logout = 13,
    LogoutAll = 14,
    CommunicationEndpointsGetAll = 15,
    CommunicationEndpointSetPreferred = 16,
    CommunicationEndpointAdd = 19,
    CommunicationEndpointSetVerified = 20,
    ChangeAccountEmail = 17,
}
