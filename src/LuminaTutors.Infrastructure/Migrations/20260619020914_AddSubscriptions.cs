using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuminaTutors.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubscriptionAddOns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddOnCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Feature = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuarterlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionAddOns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanCode = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuarterlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    YearlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IncludesAiTutor = table.Column<bool>(type: "bit", nullable: false),
                    IncludesVirtualLab = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    BillingCycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "date", nullable: false),
                    AutoRenew = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SchoolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolSubscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SchoolSubscriptionAddOns",
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
                    table.PrimaryKey("PK_SchoolSubscriptionAddOns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolSubscriptionAddOns_SchoolSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "SchoolSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SchoolSubscriptionAddOns_SubscriptionAddOns_AddOnId",
                        column: x => x.AddOnId,
                        principalTable: "SubscriptionAddOns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SubscriptionId = table.Column<int>(type: "int", nullable: false),
                    OrderCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: true),
                    BillingCycle = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "date", nullable: false),
                    PaymentMethod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TransactionCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GatewayResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SchoolId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionOrders_SchoolSubscriptions_SubscriptionId",
                        column: x => x.SubscriptionId,
                        principalTable: "SchoolSubscriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionOrders_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionOrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RefId = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionOrderItems_SubscriptionOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "SubscriptionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolSubscriptionAddOns_AddOnId",
                table: "SchoolSubscriptionAddOns",
                column: "AddOnId");

            migrationBuilder.CreateIndex(
                name: "UQ_SchoolSubscriptionAddOns_Sub_AddOn",
                table: "SchoolSubscriptionAddOns",
                columns: new[] { "SubscriptionId", "AddOnId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolSubscriptions_PlanId",
                table: "SchoolSubscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "UQ_SchoolSubscriptions_School",
                table: "SchoolSubscriptions",
                column: "SchoolId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SubscriptionAddOns_Code",
                table: "SubscriptionAddOns",
                column: "AddOnCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionOrderItems_OrderId",
                table: "SubscriptionOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionOrders_PlanId",
                table: "SubscriptionOrders",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionOrders_Status",
                table: "SubscriptionOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionOrders_SubscriptionId",
                table: "SubscriptionOrders",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "UQ_SubscriptionOrders_Code_School",
                table: "SubscriptionOrders",
                columns: new[] { "SchoolId", "OrderCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_SubscriptionPlans_Code",
                table: "SubscriptionPlans",
                column: "PlanCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SchoolSubscriptionAddOns");

            migrationBuilder.DropTable(
                name: "SubscriptionOrderItems");

            migrationBuilder.DropTable(
                name: "SubscriptionAddOns");

            migrationBuilder.DropTable(
                name: "SubscriptionOrders");

            migrationBuilder.DropTable(
                name: "SchoolSubscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");
        }
    }
}
