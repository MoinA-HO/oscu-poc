namespace src.Models;

public class ReferralMessage
{
    public Guid Id { get; set; }

    public string ReferralReference { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ReceivedDate { get; set; }

    public DateTime CreatedDate { get; set; }
}