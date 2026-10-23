namespace SklepInternetowyDocker.Services;

public class ContactMailOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Security { get; set; } = "StartTls";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderAddress { get; set; } = "contact@rp-bit.com";
    public string RecipientAddress { get; set; } = "contact@rp-bit.com";
    public bool TestMode { get; set; }
}