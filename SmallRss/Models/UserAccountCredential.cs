namespace SmallRss.Models;

public class UserAccountCredential
{
    public required byte[] CredentialId { get; set; }
    public required byte[] PublicKey { get; set; }
    public required byte[] UserHandle { get; set; }
    public uint SignatureCount { get; set; }
}