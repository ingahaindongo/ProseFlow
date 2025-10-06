using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProseFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkspaceStartupInferenceAndFloatingOrbSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpenInWindow",
                table: "Actions",
                newName: "OutputMode");

            migrationBuilder.AddColumn<bool>(
                name: "EnableFlashAttention",
                table: "ProviderSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "GpuDeviceIndex",
                table: "ProviderSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFloatingButtonHidden",
                table: "GeneralSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "StartMinimized",
                table: "GeneralSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceSyncConflictStrategy",
                table: "GeneralSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkspaceSyncMode",
                table: "GeneralSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Actions",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "GeneralSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "IsFloatingButtonHidden", "StartMinimized", "WorkspaceSyncConflictStrategy", "WorkspaceSyncMode" },
                values: new object[] { false, false, 0, 0 });

            migrationBuilder.UpdateData(
                table: "ProviderSettings",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EnableFlashAttention", "GpuDeviceIndex" },
                values: new object[] { true, -1 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnableFlashAttention",
                table: "ProviderSettings");

            migrationBuilder.DropColumn(
                name: "GpuDeviceIndex",
                table: "ProviderSettings");

            migrationBuilder.DropColumn(
                name: "IsFloatingButtonHidden",
                table: "GeneralSettings");

            migrationBuilder.DropColumn(
                name: "StartMinimized",
                table: "GeneralSettings");

            migrationBuilder.DropColumn(
                name: "WorkspaceSyncConflictStrategy",
                table: "GeneralSettings");

            migrationBuilder.DropColumn(
                name: "WorkspaceSyncMode",
                table: "GeneralSettings");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Actions");

            migrationBuilder.RenameColumn(
                name: "OutputMode",
                table: "Actions",
                newName: "OpenInWindow");
        }
    }
}
