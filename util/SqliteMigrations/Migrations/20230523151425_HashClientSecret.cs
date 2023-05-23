﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.SqliteMigrations.Migrations;

public partial class HashClientSecret : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "ClientSecret",
            table: "ApiKey");

        migrationBuilder.AddColumn<string>(
            name: "HashedClientSecret",
            table: "ApiKey",
            type: "TEXT",
            maxLength: 128,
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HashedClientSecret",
            table: "ApiKey");

        migrationBuilder.AddColumn<string>(
            name: "ClientSecret",
            table: "ApiKey",
            type: "TEXT",
            maxLength: 30,
            nullable: true);
    }
}
