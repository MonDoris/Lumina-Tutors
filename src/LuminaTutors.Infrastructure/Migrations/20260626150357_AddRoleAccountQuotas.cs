using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleAccountQuotas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mặc định -1 = không giới hạn → các gói đã tồn tại không bị chặn sau khi nâng cấp schema.
            migrationBuilder.AddColumn<int>(
                name: "MaxAccountants",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "MaxAdmins",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "MaxClasses",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "MaxParents",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "MaxStudents",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "MaxSupervisors",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: -1);

            migrationBuilder.AddColumn<int>(
                name: "MaxTeachers",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: -1);

            // Áp quota giới hạn cho gói BASIC đã tồn tại (gói cao cấp/khác giữ -1 = không giới hạn).
            migrationBuilder.Sql(@"
UPDATE SubscriptionPlans
SET MaxTeachers = 20, MaxStudents = 500, MaxParents = 500,
    MaxAdmins = 3, MaxAccountants = 2, MaxSupervisors = 2, MaxClasses = 20
WHERE PlanCode = 'BASIC';");

            migrationBuilder.CreateTable(
                name: "RoleQuotaAddOns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddOnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    TargetRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ExtraQuota = table.Column<int>(type: "int", nullable: false),
                    ExtraClasses = table.Column<int>(type: "int", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuarterlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleQuotaAddOns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolRoleQuotaAddOns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<int>(type: "int", nullable: false),
                    AddOnId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ActiveUntil = table.Column<DateTime>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolRoleQuotaAddOns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolRoleQuotaAddOns_RoleQuotaAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "RoleQuotaAddOns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolRoleQuotaAddOns_SchoolSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "SchoolSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_RoleQuotaAddOns_Code",
                table: "RoleQuotaAddOns",
                column: "AddOnCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolRoleQuotaAddOns_AddOnId",
                table: "SchoolRoleQuotaAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "UQ_SchoolRoleQuotaAddOns_Sub_AddOn",
                table: "SchoolRoleQuotaAddOns",
                columns: new[] { "SubscriptionId", "AddOnId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolRoleQuotaAddOns");

            migrationBuilder.DropTable(
                name: "RoleQuotaAddOns");

            migrationBuilder.DropColumn(
                name: "MaxAccountants",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxAdmins",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxClasses",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxParents",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxStudents",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxSupervisors",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxTeachers",
                table: "SubscriptionPlans");
        }
    }
}
