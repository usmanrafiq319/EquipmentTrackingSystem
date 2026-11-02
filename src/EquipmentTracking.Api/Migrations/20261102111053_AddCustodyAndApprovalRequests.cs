using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustodyAndApprovalRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "Equipment",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentAssetId = table.Column<string>(type: "TEXT", nullable: false),
                    BorrowerId = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedById = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeclaredLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    DecisionReason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustodyRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentAssetId = table.Column<string>(type: "TEXT", nullable: false),
                    BorrowerId = table.Column<string>(type: "TEXT", nullable: false),
                    CheckoutActorId = table.Column<string>(type: "TEXT", nullable: false),
                    CheckedOutAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    DeclaredLocation = table.Column<string>(type: "TEXT", nullable: true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    OutstandingQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    IsIndividual = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsOpen = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustodyRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_EquipmentAssetId_BorrowerId_Quantity_Status",
                table: "ApprovalRequests",
                columns: new[] { "EquipmentAssetId", "BorrowerId", "Quantity", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CustodyRecords_EquipmentAssetId_IsOpen_IsIndividual",
                table: "CustodyRecords",
                columns: new[] { "EquipmentAssetId", "IsOpen", "IsIndividual" },
                unique: true,
                filter: "\"IsOpen\" = 1 AND \"IsIndividual\" = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRequests");

            migrationBuilder.DropTable(
                name: "CustodyRecords");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Equipment");
        }
    }
}
