using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST2_2026_Team_6_FinanceWizard.Migrations
{
    /// <inheritdoc />
    public partial class AddColorAndLogo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Merchants");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Categories");
        }
    }
}
