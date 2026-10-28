using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReleaseRiskAnalyst.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReleaseSignals",
                columns: table => new
                {
                    SignalId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LinesChanged = table.Column<int>(type: "integer", nullable: false),
                    CommitCount = table.Column<int>(type: "integer", nullable: false),
                    ReviewerCount = table.Column<int>(type: "integer", nullable: false),
                    TestCoverage = table.Column<double>(type: "double precision", nullable: false),
                    FailedCiRunCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReleaseSignals", x => x.SignalId);
                });

            migrationBuilder.CreateTable(
                name: "ServiceIncidentHistories",
                columns: table => new
                {
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceIncidentHistories", x => x.IncidentId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReleaseSignals");

            migrationBuilder.DropTable(
                name: "ServiceIncidentHistories");
        }
    }
}
