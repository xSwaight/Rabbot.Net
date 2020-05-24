using Microsoft.EntityFrameworkCore.Migrations;

namespace Rabbot.Database.Migrations
{
    public partial class add_rulesettings_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RulesAcceptSettings",
                columns: table => new
                {
                    GuildId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong>(nullable: false),
                    AcceptWord = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RulesAcceptSettings", x => x.GuildId);
                    table.ForeignKey(
                        name: "FK_RulesAcceptSettings_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "GuildId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RulesAcceptSettings");
        }
    }
}
