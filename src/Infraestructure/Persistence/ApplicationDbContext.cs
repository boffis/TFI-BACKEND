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
        public DbSet<MembershipPlan> MembershipPlans { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // TPT mapping — PK defined once on root; subtypes only declare their table
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Trainer>().ToTable("Trainers");
            modelBuilder.Entity<Admin>().ToTable("Admins");

            // Membership → User (1:N — full history per user)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Memberships)
                .WithOne(m => m.User)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Membership → MembershipPlan
            modelBuilder.Entity<Membership>()
                .HasOne(m => m.MembershipPlan)
                .WithMany()
                .HasForeignKey(m => m.MembershipPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment → User (preserves payment history across role changes)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Payments)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment → Membership (Restrict — payment outlives the membership record)
            modelBuilder.Entity<Membership>()
                .HasMany(m => m.Payments)
                .WithOne(p => p.Membership)
                .HasForeignKey(p => p.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .Property(p => p.Price)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<MembershipPlan>()
                .Property(mp => mp.Price)
                .HasColumnType("decimal(10,2)");

            // GymClass → User (TrainerId points at Users, not Trainers — preserves history across role changes)
            modelBuilder.Entity<GymClass>()
                .HasOne(gc => gc.Trainer)
                .WithMany()
                .HasForeignKey(gc => gc.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // GymClassSchedule → User (same reasoning as GymClass)
            modelBuilder.Entity<GymClassSchedule>()
                .HasOne(gcs => gcs.Trainer)
                .WithMany()
                .HasForeignKey(gcs => gcs.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // GymClass → GymClassSchedule
            modelBuilder.Entity<GymClass>()
                .HasOne(gc => gc.GymClassSchedule)
                .WithMany(gcs => gcs.GymClasses)
                .HasForeignKey(gc => gc.GymClassScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            // Inscription — surrogate PK
            modelBuilder.Entity<Inscription>()
                .HasKey(i => i.InscriptionId);

            // Unique filtered index: prevent double-enrolment when ClientId is set
            modelBuilder.Entity<Inscription>()
                .HasIndex(i => new { i.ClientId, i.GymClassId })
                .IsUnique()
                .HasFilter("[ClientId] IS NOT NULL");

            // Inscription → Client (nullable, SetNull handled explicitly in service)
            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Client)
                .WithMany(c => c.Inscriptions)
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            // Inscription → GymClass
            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.GymClass)
                .WithMany(gc => gc.Inscriptions)
                .HasForeignKey(i => i.GymClassId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
