using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using BrandHandlerWebApp.Models;

namespace BrandHandlerWebApp.Data
{
    public class AppDbContext : IdentityDbContext<Users>
    {
        public AppDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<Meeting> Meetings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure BrandUser relationship
            modelBuilder.Entity<Meeting>()
                .HasOne(m => m.BrandUser)
                .WithMany()
                .HasForeignKey(m => m.BrandUserId)
                .OnDelete(DeleteBehavior.Cascade); // Or SetNull if needed

            // Configure AdminUser relationship
            modelBuilder.Entity<Meeting>()
                .HasOne(m => m.AdminUser)
                .WithMany()
                .HasForeignKey(m => m.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade conflict
        }
    }
}
