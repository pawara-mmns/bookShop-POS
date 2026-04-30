using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace bookShop.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "canManageDiscountCards",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canUsePosBilling",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewCustomers",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewDashboard",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewInventory",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewOrders",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewReports",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "canViewSuppliers",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "role",
                table: "Users",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "canManageDiscountCards",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "canUsePosBilling",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "canViewCustomers",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "canViewDashboard",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "canViewInventory",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "canViewOrders",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "canViewReports",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "canViewSuppliers",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "role",
                table: "Users");
        }
    }
}
