using FixMyTownApi.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FixMyTownApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<WorkerDepartment> WorkerDepartments => Set<WorkerDepartment>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Location> Locations => Set<Location>();
        public DbSet<Report> Reports => Set<Report>();
        public DbSet<ReportPhoto> ReportPhotos => Set<ReportPhoto>();
        public DbSet<Assignment> Assignments => Set<Assignment>();
        public DbSet<ReportStatusHistory> ReportStatusHistories => Set<ReportStatusHistory>();
        public DbSet<ProgressUpdate> ProgressUpdates => Set<ProgressUpdate>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable(tb => tb.UseSqlOutputClause(false));
            modelBuilder.Entity<Report>().ToTable(tb => tb.UseSqlOutputClause(false));
            modelBuilder.Entity<Assignment>().ToTable(tb => tb.UseSqlOutputClause(false));

            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            modelBuilder.Entity<User>().ToTable(tb => tb.UseSqlOutputClause(false));
            modelBuilder.Entity<Report>().ToTable(tb => tb.UseSqlOutputClause(false));
            modelBuilder.Entity<Assignment>().ToTable(tb => tb.UseSqlOutputClause(false));

            modelBuilder.Entity<WorkerDepartment>()
                .HasKey(wd => new { wd.UserId, wd.DepartmentId });

            modelBuilder.Entity<WorkerDepartment>()
                .HasOne(wd => wd.User)
                .WithMany(u => u.WorkerDepartments)
                .HasForeignKey(wd => wd.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkerDepartment>()
                .HasOne(wd => wd.Department)
                .WithMany(d => d.WorkerDepartments)
                .HasForeignKey(wd => wd.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Category>()
                .HasOne(c => c.Department)
                .WithMany(d => d.Categories)
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Reports)
                .HasForeignKey(r => r.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .HasOne(r => r.Location)
                .WithMany(l => l.Reports)
                .HasForeignKey(r => r.LocationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Report>()
                .Property(r => r.ResolutionTimeMinutes)
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<ReportPhoto>()
                .HasOne(p => p.Report)
                .WithMany(r => r.Photos)
                .HasForeignKey(p => p.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Report)
                .WithMany(r => r.Assignments)
                .HasForeignKey(a => a.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Worker)
                .WithMany()
                .HasForeignKey(a => a.WorkerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .Property(a => a.IsOverdue)
                .ValueGeneratedOnAddOrUpdate();

            modelBuilder.Entity<ReportStatusHistory>()
                .HasOne(h => h.Report)
                .WithMany(r => r.StatusHistory)
                .HasForeignKey(h => h.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProgressUpdate>()
                .HasOne(pu => pu.Report)
                .WithMany(r => r.ProgressUpdates)
                .HasForeignKey(pu => pu.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne<Report>()
                .WithMany()
                .HasForeignKey(n => n.ReportId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.RelatedUserId)
                .OnDelete(DeleteBehavior.Restrict);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => v,
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)));
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime?, DateTime?>(
                            v => v,
                            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v));
                    }
                }
            }
        }
    }
}