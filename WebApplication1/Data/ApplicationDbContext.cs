using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<HostProperty> HostProperties => Set<HostProperty>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<HostApplication> HostApplications => Set<HostApplication>();
    public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // keep this first: it sets up the Identity tables

        builder.Entity<HostProperty>()
            .HasOne(p => p.Host).WithMany()
            .HasForeignKey(p => p.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HostApplication>()
            .HasOne(a => a.Applicant).WithMany()
            .HasForeignKey(a => a.ApplicantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<HostApplication>()
            .HasOne(a => a.Property).WithMany()
            .HasForeignKey(a => a.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        // One check-in setup per property
        builder.Entity<CheckInProcess>()
            .HasOne(c => c.Property).WithOne(p => p.CheckInProcess)
            .HasForeignKey<CheckInProcess>(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
        // Keep booking history even if a user or property is removed
        builder.Entity<Booking>()
            .HasOne(b => b.Property).WithMany()
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Booking>()
            .HasOne(b => b.Guest).WithMany()
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}