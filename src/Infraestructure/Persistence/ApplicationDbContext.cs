using GymManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Inscription> Inscriptions { get; set; }
        public DbSet<GymClass> GymClasses { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Inscription>()
            .HasKey(i => new { i.ClientId, i.GymClassId });

            modelBuilder.Entity<Client>()
            .HasOne(c => c.Membership)
            .WithOne(m => m.Client)
            .HasForeignKey<Membership>(m => m.ClientId);

            modelBuilder.Entity<GymClass>()
            .HasOne(gc => gc.Trainer)
            .WithMany(t => t.GymClasses)
            .HasForeignKey(gc => gc.TrainerId);

            modelBuilder.Entity<Payment>()
            .Property(p => p.Price)
            .HasPrecision(10, 2);

            modelBuilder.Entity<Membership>()
            .Property(p => p.Price)
            .HasPrecision(10, 2);

        }
    }
}