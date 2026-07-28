using GymManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Infrastructure.Persistence
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<Inscription> Inscriptions { get; set; }
        public DbSet<GymClass> GymClasses { get; set; }
        public DbSet<GymClassSchedule> GymClassSchedules { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Client>().ToTable("Clients").HasKey(c => c.UserId);
            modelBuilder.Entity<Trainer>().ToTable("Trainers").HasKey(t => t.UserId);
            modelBuilder.Entity<Admin>().ToTable("Admins").HasKey(a => a.UserId);

            modelBuilder.Entity<Client>()
               .HasOne(c => c.Membership)
               .WithOne(m => m.Client)
               .HasForeignKey<Membership>(m => m.ClientId)
               .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GymClass>()
                .HasOne(gc => gc.Trainer)
                .WithMany(t => t.GymClasses)
                .HasForeignKey(gc => gc.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GymClassSchedule>()
                .HasOne(gcs => gcs.Trainer)
                .WithMany(t => t.GymClassSchedules)
                .HasForeignKey(gcs => gcs.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GymClass>()
                .HasOne(gc => gc.GymClassSchedule)
                .WithMany(gcs => gcs.GymClasses)
                .HasForeignKey(gc => gc.GymClassScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Inscription>()
                .HasKey(i => new { i.ClientId, i.GymClassId });

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Client)
                .WithMany(c => c.Inscriptions)
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.GymClass)
                .WithMany(gc => gc.Inscriptions)
                .HasForeignKey(i => i.GymClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Membership)
                .WithMany(m => m.Payments)
                .HasForeignKey(p => p.MembershipId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

        }
    }
}