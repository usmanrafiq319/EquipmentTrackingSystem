using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReturnEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CustodyRecordId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EquipmentAssetId = table.Column<string>(type: "TEXT", nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", nullable: false),
                    ReturnedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Condition = table.Column<string>(type: "TEXT", nullable: false),
                    GoodQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    DamagedQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnEvents_CustodyRecords_CustodyRecordId",
                        column: x => x.CustodyRecordId,
                        principalTable: "CustodyRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnEvents_CustodyRecordId",
                table: "ReturnEvents",
                column: "CustodyRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnEvents");
        }
    }
}
