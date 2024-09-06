﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.MySqlMigrations.Migrations;

/// <inheritdoc />
public partial class NotificationCenter : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Notification_ClientType_Global_UserId_OrganizationId_Priorit~",
            table: "Notification");

        migrationBuilder.CreateIndex(
            name: "IX_Notification_ClientType_Global_UserId_OrganizationId_Priorit~",
            table: "Notification",
            columns: new[] { "ClientType", "Global", "UserId", "OrganizationId", "Priority", "CreationDate" },
            descending: new[] { false, false, false, false, true, true });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Notification_ClientType_Global_UserId_OrganizationId_Priorit~",
            table: "Notification");

        migrationBuilder.CreateIndex(
            name: "IX_Notification_ClientType_Global_UserId_OrganizationId_Priorit~",
            table: "Notification",
            columns: new[] { "ClientType", "Global", "UserId", "OrganizationId", "Priority", "CreationDate" },
            descending: new[] { false, false, false, false, false, true });
    }
}
