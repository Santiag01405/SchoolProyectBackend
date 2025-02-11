using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Models;

namespace SchoolProyectBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<NotificationRecip> NotificationsRecip { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Definir la clave primaria de Notification
            modelBuilder.Entity<Notification>()
                .HasKey(n => n.NotifyID);

            modelBuilder.Entity<NotificationRecip>()
        .HasKey(nr => nr.Id);

            modelBuilder.Entity<User>().ToTable("user").HasKey(u => u.UserID);
            modelBuilder.Entity<Role>().ToTable("role").HasKey(r => r.RoleID);
        }
    }
}
