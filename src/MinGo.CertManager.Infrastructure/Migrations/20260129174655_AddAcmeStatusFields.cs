using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.CertManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAcmeStatusFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AcmeStatus",
                table: "Certificates",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AcmeStatusMessage",
                table: "Certificates",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcmeStatus",
                table: "Certificates");

            migrationBuilder.DropColumn(
                name: "AcmeStatusMessage",
                table: "Certificates");
        }
    }
}
