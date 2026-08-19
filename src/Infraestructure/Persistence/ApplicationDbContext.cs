using GymManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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

            // TPT mapping: PK on the root, subtypes only declare their table.
            modelBuilder.Entity<User>().HasKey(u => u.UserId);
            modelBuilder.Entity<Client>().ToTable("Clients");
            modelBuilder.Entity<Trainer>().ToTable("Trainers");
            modelBuilder.Entity<Admin>().ToTable("Admins");

            // Membership → User, 1:N so the full history is kept.
            modelBuilder.Entity<User>()
                .HasMany(u => u.Memberships)
                .WithOne(m => m.User)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Membership>()
                .HasOne(m => m.MembershipPlan)
                .WithMany()
                .HasForeignKey(m => m.MembershipPlanId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment → User, kept across role changes.
            modelBuilder.Entity<User>()
                .HasMany(u => u.Payments)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Restrict: a payment outlives its membership record.
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

            // Discontinued plans are filtered per-query in MembershipPlanRepository: a global filter
            // would drop the MembershipPlan navigation from every Membership pointing at one.

            // TrainerId points at Users, not Trainers, so history survives role changes.
            modelBuilder.Entity<GymClass>()
                .HasOne(gc => gc.Trainer)
                .WithMany()
                .HasForeignKey(gc => gc.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Same reasoning as GymClass above.
            modelBuilder.Entity<GymClassSchedule>()
                .HasOne(gcs => gcs.Trainer)
                .WithMany()
                .HasForeignKey(gcs => gcs.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GymClass>()
                .HasOne(gc => gc.GymClassSchedule)
                .WithMany(gcs => gcs.GymClasses)
                .HasForeignKey(gc => gc.GymClassScheduleId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Inscription>()
                .HasKey(i => i.InscriptionId);

            // Filtered unique index: no double-enrolment while ClientId is set.
            modelBuilder.Entity<Inscription>()
                .HasIndex(i => new { i.ClientId, i.GymClassId })
                .IsUnique()
                .HasFilter("[ClientId] IS NOT NULL");

            // Nullable; SetNull is handled explicitly in the service.
            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.Client)
                .WithMany(c => c.Inscriptions)
                .HasForeignKey(i => i.ClientId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Inscription>()
                .HasOne(i => i.GymClass)
                .WithMany(gc => gc.Inscriptions)
                .HasForeignKey(i => i.GymClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // datetime2 keeps no offset, so EF reads these back as Unspecified and the serialiser
            // drops the "Z" — browsers then read them as local time. Re-tag on the way out.
            // Only real instants belong here: GymClass.Schedule and GymClassSchedule.TimeOfDay are
            // the gym's wall clock (see GymTime.cs) and must stay zone-less.
            var utcInstant = new ValueConverter<DateTime, DateTime>(
                write => write,
                read => DateTime.SpecifyKind(read, DateTimeKind.Utc));

            modelBuilder.Entity<Payment>()
                .Property(p => p.PaymentDate)
                .HasConversion(utcInstant);

            modelBuilder.Entity<Membership>()
                .Property(m => m.ExpirationDate)
                .HasConversion(utcInstant);

            // Non-nullable converter on a nullable property: EF applies it only to non-null values.
            modelBuilder.Entity<Inscription>()
                .Property(i => i.AttendanceRecordedAt)
                .HasConversion(utcInstant);
        }
    }
}
