using KarttaBackEnd2.Server.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace KarttaBackEnd2.Server.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public DbSet<Waypoint> Waypoints { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
