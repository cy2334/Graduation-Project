using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrpcTestService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrderEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WarehouseId",
                table: "Orders",
                newName: "WarehouseBId");

            migrationBuilder.AddColumn<int>(
                name: "WarehouseAId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WarehouseAId",
                table: "Orders");

            migrationBuilder.RenameColumn(
                name: "WarehouseBId",
                table: "Orders",
                newName: "WarehouseId");
        }
    }
}
