namespace Cross.Identity.ProcessEngine.Core.Enums;

/// <summary>
/// Поддерживаемые операции флоу.
/// </summary>
public enum FlowOperationEnum
{
    Register,
    Token,
    TokenByCode,
    RefreshToken,
    RequestCode,
    ResetPassword,
    ForgotPassword,
}
