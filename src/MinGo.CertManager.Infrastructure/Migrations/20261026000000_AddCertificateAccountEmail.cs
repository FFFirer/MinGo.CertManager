using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.CertManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateAccountEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountEmail",
                table: "Certificates",
                type: "TEXT",
                maxLength: 255,
                nullable: true,
                comment: "ACME 账户邮箱标识；为空时使用部署配置默认值");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountEmail",
                table: "Certificates");
        }
    }
}
