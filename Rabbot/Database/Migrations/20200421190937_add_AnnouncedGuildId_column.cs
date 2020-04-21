using Microsoft.EntityFrameworkCore.Migrations;

namespace Rabbot.Database.Migrations
{
    public partial class add_AnnouncedGuildId_column : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "AnnouncedGuildId",
                table: "Streams",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.CreateIndex(
                name: "IX_Streams_AnnouncedGuildId",
                table: "Streams",
                column: "AnnouncedGuildId");

            migrationBuilder.AddForeignKey(
                name: "FK_Streams_Guilds_AnnouncedGuildId",
                table: "Streams",
                column: "AnnouncedGuildId",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Streams_Guilds_AnnouncedGuildId",
                table: "Streams");

            migrationBuilder.DropIndex(
                name: "IX_Streams_AnnouncedGuildId",
                table: "Streams");

            migrationBuilder.DropColumn(
                name: "AnnouncedGuildId",
                table: "Streams");
        }
    }
}
