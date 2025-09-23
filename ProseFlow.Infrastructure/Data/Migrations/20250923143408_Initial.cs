using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActionGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActionGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CloudProviderConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApiKey = table.Column<string>(type: "TEXT", nullable: false),
                    BaseUrl = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Temperature = table.Column<float>(type: "REAL", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudProviderConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GeneralSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ActionMenuHotkey = table.Column<string>(type: "TEXT", nullable: false),
                    SmartPasteHotkey = table.Column<string>(type: "TEXT", nullable: false),
                    SmartPasteActionId = table.Column<int>(type: "INTEGER", nullable: true),
                    LaunchAtLogin = table.Column<bool>(type: "INTEGER", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: false),
                    IsOnboardingCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "History",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActionName = table.Column<string>(type: "TEXT", nullable: false),
                    InputText = table.Column<string>(type: "TEXT", nullable: false),
                    OutputText = table.Column<string>(type: "TEXT", nullable: false),
                    ProviderUsed = table.Column<string>(type: "TEXT", nullable: false),
                    ModelUsed = table.Column<string>(type: "TEXT", nullable: false),
                    PromptTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletionTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    LatencyMs = table.Column<double>(type: "REAL", nullable: false),
                    TokensPerSecond = table.Column<double>(type: "REAL", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_History", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LocalModels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Creator = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Tag = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    FileSizeGb = table.Column<double>(type: "REAL", nullable: false),
                    IsManaged = table.Column<bool>(type: "INTEGER", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProviderSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LocalModelPath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalCpuCores = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalModelContextSize = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalModelMaxTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalModelTemperature = table.Column<float>(type: "REAL", nullable: false),
                    PreferGpu = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalModelLoadOnStartup = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalModelAutoUnloadEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalModelIdleTimeoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    LocalModelMemoryMap = table.Column<bool>(type: "INTEGER", nullable: false),
                    LocalModelMemorylock = table.Column<bool>(type: "INTEGER", nullable: false),
                    PrimaryServiceType = table.Column<string>(type: "TEXT", nullable: false),
                    FallbackServiceType = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UsageStatistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    PromptTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletionTokens = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageStatistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Actions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Prefix = table.Column<string>(type: "TEXT", nullable: false),
                    Instruction = table.Column<string>(type: "TEXT", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", nullable: false),
                    OpenInWindow = table.Column<bool>(type: "INTEGER", nullable: false),
                    ExplainChanges = table.Column<bool>(type: "INTEGER", nullable: false),
                    ApplicationContext = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionGroupId = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actions_ActionGroups_ActionGroupId",
                        column: x => x.ActionGroupId,
                        principalTable: "ActionGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "ActionGroups",
                columns: new[] { "Id", "CreatedAtUtc", "Name", "SortOrder", "UpdatedAtUtc" },
                values: new object[] { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "General", 0, null });

            migrationBuilder.InsertData(
                table: "GeneralSettings",
                columns: new[] { "Id", "ActionMenuHotkey", "CreatedAtUtc", "IsOnboardingCompleted", "LaunchAtLogin", "SmartPasteActionId", "SmartPasteHotkey", "Theme", "UpdatedAtUtc" },
                values: new object[] { 1, "Ctrl+J", new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), false, false, null, "Ctrl+Shift+V", "System", null });

            migrationBuilder.InsertData(
                table: "ProviderSettings",
                columns: new[] { "Id", "CreatedAtUtc", "FallbackServiceType", "LocalCpuCores", "LocalModelAutoUnloadEnabled", "LocalModelContextSize", "LocalModelIdleTimeoutMinutes", "LocalModelLoadOnStartup", "LocalModelMaxTokens", "LocalModelMemoryMap", "LocalModelMemorylock", "LocalModelPath", "LocalModelTemperature", "PreferGpu", "PrimaryServiceType", "UpdatedAtUtc" },
                values: new object[] { 1, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "None", 4, true, 4096, 30, false, 2048, true, false, "", 0.7f, true, "Cloud", null });

            migrationBuilder.CreateIndex(
                name: "IX_Actions_ActionGroupId",
                table: "Actions",
                column: "ActionGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalModels_FilePath",
                table: "LocalModels",
                column: "FilePath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsageStatistics_Year_Month",
                table: "UsageStatistics",
                columns: new[] { "Year", "Month" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actions");

            migrationBuilder.DropTable(
                name: "CloudProviderConfigurations");

            migrationBuilder.DropTable(
                name: "GeneralSettings");

            migrationBuilder.DropTable(
                name: "History");

            migrationBuilder.DropTable(
                name: "LocalModels");

            migrationBuilder.DropTable(
                name: "ProviderSettings");

            migrationBuilder.DropTable(
                name: "UsageStatistics");

            migrationBuilder.DropTable(
                name: "ActionGroups");
        }
    }
}
