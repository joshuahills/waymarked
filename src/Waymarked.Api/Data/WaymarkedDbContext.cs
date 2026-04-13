namespace Waymarked.Api.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class WaymarkedDbContext(DbContextOptions<WaymarkedDbContext> options)
    : IdentityDbContext<ApplicationUser>(options);
