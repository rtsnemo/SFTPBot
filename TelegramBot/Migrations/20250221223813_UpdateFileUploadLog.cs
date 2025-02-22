using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TelegramBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFileUploadLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "FileUploadLogs",
                newName: "TelegramId");

            migrationBuilder.AddColumn<string>(
                name: "RemotePath",
                table: "FileUploadLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Secret",
                table: "FileUploadLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SecretKeyParam",
                table: "FileUploadLogs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemotePath",
                table: "FileUploadLogs");

            migrationBuilder.DropColumn(
                name: "Secret",
                table: "FileUploadLogs");

            migrationBuilder.DropColumn(
                name: "SecretKeyParam",
                table: "FileUploadLogs");

            migrationBuilder.RenameColumn(
                name: "TelegramId",
                table: "FileUploadLogs",
                newName: "UserId");
        }
    }
}
