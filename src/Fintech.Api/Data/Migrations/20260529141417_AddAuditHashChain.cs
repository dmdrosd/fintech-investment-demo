using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fintech.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditHashChain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentHash",
                table: "audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PayloadHash",
                table: "audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviousHash",
                table: "audit_logs",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_CurrentHash",
                table: "audit_logs",
                column: "CurrentHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_audit_logs_CurrentHash",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "CurrentHash",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "PayloadHash",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "PreviousHash",
                table: "audit_logs");
        }
    }
}
