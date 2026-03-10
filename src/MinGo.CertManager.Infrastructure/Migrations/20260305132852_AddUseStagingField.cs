using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.CertManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUseStagingField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "UseStaging",
                table: "Certificates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseStaging",
                table: "Certificates");
        }
    }
}
