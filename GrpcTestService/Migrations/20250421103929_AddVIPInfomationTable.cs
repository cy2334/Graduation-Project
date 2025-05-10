using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrpcTestService.Migrations
{
    /// <inheritdoc />
    public partial class AddVIPInfomationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VIPAndWarehouseRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VIPId = table.Column<int>(type: "int", nullable: false),
                    WarehouseId = table.Column<int>(type: "int", nullable: false),
                    OccupiesVolume = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VIPAndWarehouseRelations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VIPInfomations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    recharge = table.Column<int>(type: "int", nullable: false),
                    Dailyspending = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VIPInfomations", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VIPAndWarehouseRelations");

            migrationBuilder.DropTable(
                name: "VIPInfomations");
        }
    }
}
