using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoloDevBoard.Inventory.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Inventory_CanonicalFundingStandard",
                columns: table => new
                {
                    OperatorKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SavedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory_CanonicalFundingStandard", x => x.OperatorKey);
                });

            migrationBuilder.CreateTable(
                name: "Inventory_InventoryRun",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Trigger = table.Column<int>(type: "int", nullable: false),
                    AccessKind = table.Column<int>(type: "int", nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    WasRateLimited = table.Column<bool>(type: "bit", nullable: false),
                    StandardContentHashAtStart = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IntendedRepositoryCount = table.Column<int>(type: "int", nullable: false),
                    CheckedRepositoryCount = table.Column<int>(type: "int", nullable: false),
                    IsSuperseded = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory_InventoryRun", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inventory_RepositoryInventorySnapshot",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperatorKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RepositoryFullName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false),
                    IsFork = table.Column<bool>(type: "bit", nullable: false),
                    IsPrivate = table.Column<bool>(type: "bit", nullable: false),
                    InstallationVisible = table.Column<bool>(type: "bit", nullable: false),
                    ScanState = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    FundingStatus = table.Column<int>(type: "int", nullable: true),
                    ComparedPath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NotedExtraFundingPath = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    FundingPlatforms = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    OrganisationDefaultNoted = table.Column<bool>(type: "bit", nullable: false),
                    HasDescription = table.Column<bool>(type: "bit", nullable: true),
                    HasReadme = table.Column<bool>(type: "bit", nullable: true),
                    CodeOfConductInRepository = table.Column<bool>(type: "bit", nullable: true),
                    CodeOfConductOrganisationOnly = table.Column<bool>(type: "bit", nullable: true),
                    ContributingInRepository = table.Column<bool>(type: "bit", nullable: true),
                    ContributingOrganisationOnly = table.Column<bool>(type: "bit", nullable: true),
                    LicenceInRepository = table.Column<bool>(type: "bit", nullable: true),
                    LicenceOrganisationOnly = table.Column<bool>(type: "bit", nullable: true),
                    LicenceName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SecurityPolicyInRepository = table.Column<bool>(type: "bit", nullable: true),
                    SecurityPolicyOrganisationOnly = table.Column<bool>(type: "bit", nullable: true),
                    IssueTemplatesInRepository = table.Column<bool>(type: "bit", nullable: true),
                    IssueTemplatesOrganisationOnly = table.Column<bool>(type: "bit", nullable: true),
                    PullRequestTemplateInRepository = table.Column<bool>(type: "bit", nullable: true),
                    PullRequestTemplateOrganisationOnly = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventory_RepositoryInventorySnapshot", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventory_RepositoryInventorySnapshot_Inventory_InventoryRun_InventoryRunId",
                        column: x => x.InventoryRunId,
                        principalTable: "Inventory_InventoryRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_InventoryRun_OperatorKey",
                table: "Inventory_InventoryRun",
                column: "OperatorKey",
                unique: true,
                filter: "[Outcome] IN (0, 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_InventoryRun_OperatorKey_FinishedAtUtc",
                table: "Inventory_InventoryRun",
                columns: new[] { "OperatorKey", "FinishedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_RepositoryInventorySnapshot_InventoryRunId_RepositoryFullName",
                table: "Inventory_RepositoryInventorySnapshot",
                columns: new[] { "InventoryRunId", "RepositoryFullName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventory_RepositoryInventorySnapshot_OperatorKey",
                table: "Inventory_RepositoryInventorySnapshot",
                column: "OperatorKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inventory_CanonicalFundingStandard");

            migrationBuilder.DropTable(
                name: "Inventory_RepositoryInventorySnapshot");

            migrationBuilder.DropTable(
                name: "Inventory_InventoryRun");
        }
    }
}
