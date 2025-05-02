using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using GreenMeadowsPortal.Models;

namespace GreenMeadowsPortal.Data
{
    // Change from DbContext to IdentityDbContext<ApplicationUser>
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        // Existing DbSets
        public DbSet<AdminAnnouncement> Announcements { get; set; }
        public DbSet<AnnouncementReadReceipt> AnnouncementReadReceipts { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ActivityLog> ActivityLogs { get; set; }

        // Add these new DbSets
        public DbSet<FaceEnrollment> FaceEnrollments { get; set; }
        public DbSet<TimeRecord> TimeRecords { get; set; }
        public DbSet<PayPeriod> PayPeriods { get; set; }
        public DbSet<EmployeePayment> EmployeePayments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // You can customize the Identity model here if needed
            // For example, change table names:
            // builder.Entity<ApplicationUser>().ToTable("AppUsers");
            // builder.Entity<IdentityRole>().ToTable("AppRoles");
        }
    }
}