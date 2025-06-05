using Microsoft.EntityFrameworkCore;

namespace KTGiuaKy.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<NganhHoc> NganhHocs { get; set; }
        public DbSet<SinhVien> SinhViens { get; set; }
        public DbSet<HocPhan> HocPhans { get; set; }
        public DbSet<DangKy> DangKys { get; set; }
        public DbSet<ChiTietDangKy> ChiTietDangKys { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChiTietDangKy>()
                .HasKey(ct => new { ct.MaDK, ct.MaHP });

            // Ánh xạ tên bảng rõ ràng
            modelBuilder.Entity<NganhHoc>().ToTable("NganhHoc");
            modelBuilder.Entity<SinhVien>().ToTable("SinhVien");
            modelBuilder.Entity<HocPhan>().ToTable("HocPhan");
            modelBuilder.Entity<DangKy>().ToTable("DangKy");
            modelBuilder.Entity<ChiTietDangKy>().ToTable("ChiTietDangKy");
        }
    }
}