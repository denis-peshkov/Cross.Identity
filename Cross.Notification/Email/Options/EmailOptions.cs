namespace Cross.Notification.Email.Options;

public class EmailOptions
{
    public string SmtpHost { get; set; }

    public int SmtpPort { get; set; }

    public bool UseSsl { get; set; }

    /// <summary>
    /// 465 => SecureSocketOptions.SslOnConnect,
    /// 587 => SecureSocketOptions.StartTls,
    /// 25  => SecureSocketOptions.None,
    /// _   => SecureSocketOptions.Auto
    /// </summary>
    public SecureSocketOptions SecureSocket { get; set; }

    public string SmtpLogin { get; set; }

    public string SmtpPassword { get; set; }

    public string FromUserName { get; set; }

    public string FromUserAddress { get; set; }
}
