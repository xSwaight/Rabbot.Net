using Microsoft.EntityFrameworkCore.Migrations;

namespace Rabbot.Database.Migrations
{
    public partial class add_roleid_to_rulesettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "RoleId",
                table: "RulesAcceptSettings",
                nullable: false,
                defaultValue: 0ul);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "RulesAcceptSettings");
        }
    }
}
