using Microsoft.AspNetCore.Identity;

namespace CyberServer.Domain;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
    public ICollection<UserClubAccess> ClubAccess { get; set; } = [];
}
