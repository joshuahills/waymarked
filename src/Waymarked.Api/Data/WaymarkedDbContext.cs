using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Waymarked.Api.Data;

public class WaymarkedDbContext(DbContextOptions<WaymarkedDbContext> options)
    : IdentityDbContext<ApplicationUser>(options);
