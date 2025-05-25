using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Buoi5.Migrations
{
    /// <inheritdoc />
    public partial class ThemDulieumau : Migration
    {
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			// Thêm dữ liệu vào bảng Grades
			migrationBuilder.InsertData(
				table: "Grades",
				columns: ["GradeId", "GradeName"],
				values: [1, "210THA1"]
			);

			migrationBuilder.InsertData(
				table: "Grades",
				columns: ["GradeId", "GradeName"],
				values: [2, "210THA2"]
			);

			migrationBuilder.InsertData(
				table: "Grades",
				columns: ["GradeId", "GradeName"],
				values: [3, "210THA3"]
			);

			// Thêm dữ liệu vào bảng Students
			migrationBuilder.InsertData(
				table: "Students",
				columns: ["StudentId", "FirstName", "LastName", "GradeId"],
				values: [1, "Khuyến", "Bội", 1]
			);

			migrationBuilder.InsertData(
				table: "Students",
				columns: ["StudentId", "FirstName", "LastName", "GradeId"],
				values: [2, "Toàn", "Nguyễn", 1]
			);

			// Thêm các bản ghi sinh viên khác ở đây
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
