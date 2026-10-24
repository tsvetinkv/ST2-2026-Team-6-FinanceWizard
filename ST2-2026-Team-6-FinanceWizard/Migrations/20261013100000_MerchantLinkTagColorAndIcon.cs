using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ST2_2026_Team_6_FinanceWizard.Repositories;

#nullable disable

namespace ST2_2026_Team_6_FinanceWizard.Migrations
{
    /// <summary>
    /// Merchants lose their colour (they use an automatic colour now).
    /// Tags get a required colour and an optional icon.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20261013100000_MerchantLinkTagColorAndIcon")]
    public partial class MerchantLinkTagColorAndIcon : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Color",
                table: "Merchants");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "#1d4ed8");

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Tags",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Tags");

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Merchants",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
