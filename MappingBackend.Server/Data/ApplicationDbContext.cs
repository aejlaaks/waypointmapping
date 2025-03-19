using KarttaBackEnd2.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace KarttaBackEnd2.Server.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Waypoint> Waypoints { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
