using Microsoft.EntityFrameworkCore.Migrations;

namespace Rabbot.Database.Migrations
{
    public partial class streams_announcedguildid_nullable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Streams_Guilds_AnnouncedGuildId",
                table: "Streams");

            migrationBuilder.AlterColumn<ulong>(
                name: "AnnouncedGuildId",
                table: "Streams",
                nullable: true,
                oldClrType: typeof(ulong),
                oldType: "bigint unsigned");

            migrationBuilder.AddForeignKey(
                name: "FK_Streams_Guilds_AnnouncedGuildId",
                table: "Streams",
                column: "AnnouncedGuildId",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Streams_Guilds_AnnouncedGuildId",
                table: "Streams");

            migrationBuilder.AlterColumn<ulong>(
                name: "AnnouncedGuildId",
                table: "Streams",
                type: "bigint unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Streams_Guilds_AnnouncedGuildId",
                table: "Streams",
                column: "AnnouncedGuildId",
                principalTable: "Guilds",
                principalColumn: "GuildId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
