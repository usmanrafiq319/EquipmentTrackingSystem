using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquipmentTracking.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApprovalRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipmentAssetId = table.Column<string>(type: "TEXT", nullable: false),
                    ApproverEmail = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRules_EquipmentAssetId_ApproverEmail",
                table: "ApprovalRules",
                columns: new[] { "EquipmentAssetId", "ApproverEmail" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRules");
        }
    }
}
