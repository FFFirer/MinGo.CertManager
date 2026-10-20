using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MinGo.CertManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DomainRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ProviderRecordId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Rr = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    RecordType = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Value = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: false),
                    Ttl = table.Column<int>(type: "INTEGER", nullable: false),
                    Fqdn = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Domains",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    Fqdn = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    ZoneName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Source = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Remark = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Domains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DomainZones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    ZoneName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ProviderZoneId = table.Column<string>(type: "TEXT", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DomainZones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Actor = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    Action = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetType = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    TargetId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    TargetName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: true),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    Result = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    TraceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Payload = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DomainRecords_Fqdn",
                table: "DomainRecords",
                column: "Fqdn");

            migrationBuilder.CreateIndex(
                name: "IX_DomainRecords_ProviderType_ZoneName_ProviderRecordId",
                table: "DomainRecords",
                columns: new[] { "ProviderType", "ZoneName", "ProviderRecordId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Domains_ProviderType_Fqdn",
                table: "Domains",
                columns: new[] { "ProviderType", "Fqdn" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Domains_Status",
                table: "Domains",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Domains_ZoneName",
                table: "Domains",
                column: "ZoneName");

            migrationBuilder.CreateIndex(
                name: "IX_DomainZones_ProviderType_ZoneName",
                table: "DomainZones",
                columns: new[] { "ProviderType", "ZoneName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_Action",
                table: "OperationLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_At",
                table: "OperationLogs",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_OperationLogs_TargetName",
                table: "OperationLogs",
                column: "TargetName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DomainRecords");

            migrationBuilder.DropTable(
                name: "Domains");

            migrationBuilder.DropTable(
                name: "DomainZones");

            migrationBuilder.DropTable(
                name: "OperationLogs");
        }
    }
}
