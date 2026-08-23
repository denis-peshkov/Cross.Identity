namespace Cross.Identity;

public static class ClaimConstants
{
    public static string Username => "username";

    public static string Permission => "permission";

    /// <summary>
    /// Current <c>UserAccount.SecurityStamp</c> embedded in access/refresh JWTs.
    /// Compared to the DB on validate; rotates with password change / OAuth unlink.
    /// </summary>
    public static string SecurityStamp => "security_stamp";
}
