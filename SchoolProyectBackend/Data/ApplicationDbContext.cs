using Microsoft.EntityFrameworkCore;
using SchoolProyectBackend.Models;
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
        public DbSet<Evaluation> Evaluations { get; set; }
        public DbSet<UserRelationship> UserRelationships { get; set; }
        public DbSet<Attendance> Attendance { get; set; }
        public DbSet<School> Schools { get; set; }
        public DbSet<Classroom> Classrooms { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Definir la clave primaria de Notification
            modelBuilder.Entity<Notification>().HasKey(n => n.NotifyID);
            modelBuilder.Entity<NotificationRecip>().ToTable("notifyRecip").HasKey(nr => nr.RecipientId);

            modelBuilder.Entity<NotificationRecip>()
                .Property(nr => nr.RecipientId)
                .HasColumnName("recipientID");

            modelBuilder.Entity<Notification>()
                .Property(n => n.NotifyID)
                .HasColumnName("notificationID");


            // 🔹 Configurar User y asegurarse de que la tabla es "user"
            modelBuilder.Entity<User>().ToTable("user").HasKey(u => u.UserID);
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleID)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Configurar Teacher y su relación con User
          /*  modelBuilder.Entity<Teacher>().ToTable("teacher").HasKey(t => t.TeacherID);
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithMany()
                .HasForeignKey(t => t.UserID)
                .OnDelete(DeleteBehavior.Restrict);*/

            // 🔹 Configurar Student y su relación con User y Parent
            modelBuilder.Entity<Student>().ToTable("student").HasKey(s => s.StudentID);
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Parent)
                .WithMany(p => p.Students)
                .HasForeignKey(s => s.ParentID)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Configurar Parent y su relación con User
            modelBuilder.Entity<Parent>().ToTable("parent").HasKey(p => p.ParentID);
            modelBuilder.Entity<Parent>()
                .HasOne(p => p.User)
                .WithMany()
                .HasForeignKey(p => p.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            // 🔹 Configurar Course y su relación con User (que es el profesor)
            modelBuilder.Entity<Course>()
                .ToTable("courses")
                .HasKey(c => c.CourseID);

            modelBuilder.Entity<Course>()
                .HasOne(c => c.User) // Relación con la tabla `User`
                .WithMany(u => u.Courses) // Un usuario (profesor) puede tener muchos cursos
                .HasForeignKey(c => c.UserID) // Clave foránea es `UserID`
                .OnDelete(DeleteBehavior.Restrict);


            // 🔹 Configurar Course y su relación con Teacher
            /* modelBuilder.Entity<Course>().ToTable("courses").HasKey(c => c.CourseID);
             modelBuilder.Entity<Course>()
                 .HasOne(c => c.Teacher)
                 .WithMany(t => t.Courses)
                 .HasForeignKey(c => c.TeacherID)
                 .OnDelete(DeleteBehavior.Restrict);*/

            // 🔹 Configurar Enrollment y su relación con User y Course
            modelBuilder.Entity<Enrollment>().ToTable("enrollment").HasKey(e => e.EnrollmentID);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments) 
                .HasForeignKey(e => e.UserID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments) 
                .HasForeignKey(e => e.CourseID)
                .OnDelete(DeleteBehavior.Restrict);




            // 🔹 Configurar Grade y su relación con Student y Course
            modelBuilder.Entity<Grade>().ToTable("grades").HasKey(g => g.GradeID);
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentID)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Course)
                .WithMany(c => c.Grades)
                .HasForeignKey(g => g.CourseID)
                .OnDelete(DeleteBehavior.Restrict);

            //Evaluaciones
            modelBuilder.Entity<Evaluation>()
           .ToTable("Evaluations");

            //Relaciones
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserRelationship>()
                .HasIndex(ur => new { ur.User1ID, ur.User2ID, ur.RelationshipType })
                .IsUnique();

            // Tabla y clave primaria
            modelBuilder.Entity<School>().ToTable("schools").HasKey(s => s.SchoolID);

            // Relación User -> School
            modelBuilder.Entity<User>()
                .HasOne(u => u.School)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Course -> School
            modelBuilder.Entity<Course>()
                .HasOne(c => c.School)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Tabla schools
            modelBuilder.Entity<School>().ToTable("schools").HasKey(s => s.SchoolID);

            // Relación User → School
            modelBuilder.Entity<User>()
                .HasOne(u => u.School)
                .WithMany(s => s.Users)
                .HasForeignKey(u => u.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Course → School
            modelBuilder.Entity<Course>()
                .HasOne(c => c.School)
                .WithMany(s => s.Courses)
                .HasForeignKey(c => c.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Enrollment → School
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.School)
                .WithMany(s => s.Enrollments)
                .HasForeignKey(e => e.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Attendance → School
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.School)
                .WithMany(s => s.Attendance)
                .HasForeignKey(a => a.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Evaluation → School
            modelBuilder.Entity<Evaluation>()
                .HasOne(e => e.School)
                .WithMany(s => s.Evaluations)
                .HasForeignKey(e => e.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación Notification → School
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.School)
                .WithMany(s => s.Notifications)
                .HasForeignKey(n => n.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación UserRelationship → School
            modelBuilder.Entity<UserRelationship>()
                .HasOne(ur => ur.School)
                .WithMany(s => s.UserRelationships)
                .HasForeignKey(ur => ur.SchoolID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<User>()
    .HasOne(u => u.School)
    .WithMany(s => s.Users)
    .HasForeignKey(u => u.SchoolID);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Classroom)
                .WithMany(c => c.Users)
                .HasForeignKey(u => u.ClassroomID)
                .OnDelete(DeleteBehavior.SetNull);


        }
    }
}
