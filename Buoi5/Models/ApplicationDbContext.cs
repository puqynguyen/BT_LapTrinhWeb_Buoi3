using Microsoft.EntityFrameworkCore;

namespace Buoi5.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Grade> Grades { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình quan hệ giữa Student và Grade
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Grade)
                .WithMany(g => g.Students)
                .HasForeignKey(s => s.GradeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Đảm bảo các trường bắt buộc
            modelBuilder.Entity<Student>()
                .Property(s => s.FirstName)
                .IsRequired();

            modelBuilder.Entity<Student>()
                .Property(s => s.LastName)
                .IsRequired();

            modelBuilder.Entity<Student>()
                .Property(s => s.GradeId)
                .IsRequired();

            modelBuilder.Entity<Grade>()
                .Property(g => g.GradeName)
                .IsRequired();
        }
    }
}