using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemAdminRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quản trị toàn bộ một trường: học vụ, nhân sự, tài chính, gói dịch vụ của trường", "Nhà trường" });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "RoleId", "Description", "RoleCode", "RoleName" },
                values: new object[] { 7, "Quản trị hệ thống: toàn quyền mọi chức năng + quản lý gói E-Selling (catalog gói/add-on, đăng ký của mọi trường)", "SYSADMIN", "Quản trị viên" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 7);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                columns: new[] { "Description", "RoleName" },
                values: new object[] { "Quản trị toàn bộ hệ thống", "Quản trị viên" });
        }
    }
}
