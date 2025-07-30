using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BrandHandlerWebApp.Models;

namespace BrandHandlerWebApp.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions options ) : base(options)
        {

        }
        
        public DbSet<Meeting> Meetings { get; set; }
    }
}
