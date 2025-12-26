using Microsoft.EntityFrameworkCore;
using FortuneCookie.Shared;
using System.IO;

namespace FortuneCookie.Server
{
    public class FortuneDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Fortune> Fortunes { get; set; }
        public DbSet<FortuneHistory> FortuneHistories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=fortune.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasKey(u => u.Id);
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
            modelBuilder.Entity<Fortune>().HasKey(f => f.Id);
            modelBuilder.Entity<Fortune>().Ignore(f => f.LuckyNumbers);
        }
    }
}
