namespace CyberServer.Domain;

public class UserClubAccess
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public Guid ClubId { get; set; }
    public Club Club { get; set; } = null!;
}
