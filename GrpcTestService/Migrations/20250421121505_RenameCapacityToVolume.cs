using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GrpcTestService.Migrations
{
    /// <inheritdoc />
    public partial class RenameCapacityToVolume : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "capacity",
                table: "Warehouses");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Warehouses",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "address",
                table: "Warehouses",
                newName: "Address");

            migrationBuilder.AddColumn<int>(
                name: "Volume",
                table: "Warehouses",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Volume",
                table: "Warehouses");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Warehouses",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Warehouses",
                newName: "address");

            migrationBuilder.AddColumn<string>(
                name: "capacity",
                table: "Warehouses",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
