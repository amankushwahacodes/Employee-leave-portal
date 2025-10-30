using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using EmployeeLeavePortal.Models;
using Microsoft.AspNetCore.Identity;

namespace EmployeeLeavePortal.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
    public DbSet<LeaveBalance> LeaveBalances => Set<LeaveBalance>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Identity for MySQL: set explicit lengths for keys and indexed columns
        builder.Entity<ApplicationUser>(b =>
        {
            b.Property(u => u.Id).HasMaxLength(255);
            b.Property(u => u.UserName).HasMaxLength(256);
            b.Property(u => u.NormalizedUserName).HasMaxLength(256);
            b.Property(u => u.Email).HasMaxLength(256);
            b.Property(u => u.NormalizedEmail).HasMaxLength(256);
        });

        builder.Entity<IdentityRole>(b =>
        {
            b.Property(r => r.Id).HasMaxLength(255);
            b.Property(r => r.Name).HasMaxLength(256);
            b.Property(r => r.NormalizedName).HasMaxLength(256);
        });

        builder.Entity<IdentityUserLogin<string>>(b =>
        {
            b.Property(l => l.LoginProvider).HasMaxLength(128);
            b.Property(l => l.ProviderKey).HasMaxLength(128);
        });

        builder.Entity<IdentityUserToken<string>>(b =>
        {
            b.Property(t => t.LoginProvider).HasMaxLength(128);
            b.Property(t => t.Name).HasMaxLength(128);
        });

        builder.Entity<IdentityUserRole<string>>(b =>
        {
            b.Property(ur => ur.UserId).HasMaxLength(255);
            b.Property(ur => ur.RoleId).HasMaxLength(255);
        });

        builder.Entity<IdentityUserClaim<string>>(b =>
        {
            b.Property(uc => uc.Id).ValueGeneratedOnAdd();
            b.Property(uc => uc.UserId).HasMaxLength(255);
        });

        builder.Entity<IdentityRoleClaim<string>>(b =>
        {
            b.Property(rc => rc.Id).ValueGeneratedOnAdd();
            b.Property(rc => rc.RoleId).HasMaxLength(255);
        });

        builder.Entity<Department>()
            .HasMany(d => d.Users)
            .WithOne(u => u.Department)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<AttendanceRecord>()
            .HasOne(a => a.Employee)
            .WithMany(u => u.AttendanceRecords)
            .HasForeignKey(a => a.EmployeeId)
            .IsRequired();

        builder.Entity<LeaveRequest>()
            .HasOne(l => l.Employee)
            .WithMany(u => u.LeaveRequests)
            .HasForeignKey(l => l.EmployeeId)
            .IsRequired();

        builder.Entity<LeaveBalance>()
            .HasIndex(lb => new { lb.EmployeeId, lb.LeaveType })
            .IsUnique();
        builder.Entity<LeaveBalance>()
            .HasOne(lb => lb.Employee)
            .WithMany(u => u.LeaveBalances)
            .HasForeignKey(lb => lb.EmployeeId)
            .IsRequired();
    }
}
