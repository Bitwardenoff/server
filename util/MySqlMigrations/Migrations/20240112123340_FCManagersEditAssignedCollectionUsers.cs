﻿using Bit.Core.Utilities;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.MySqlMigrations.Migrations;

public partial class FCManagersEditAssignedCollectionUsers : Migration
{
    private const string _managersEditAssignedCollectionUsersScript = "MySqlMigrations.HelperScripts.2024-02-16_02_ManagersEditAssignedCollectionUsers.sql";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(CoreHelpers.GetEmbeddedResourceContentsAsync(_managersEditAssignedCollectionUsersScript));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new Exception("Irreversible migration");
    }
}
