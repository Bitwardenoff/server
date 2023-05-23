﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.MySqlMigrations.Migrations;

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
            type: "varchar(128)",
            maxLength: 128,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "HashedClientSecret",
            table: "ApiKey");

        migrationBuilder.AddColumn<string>(
            name: "ClientSecret",
            table: "ApiKey",
            type: "varchar(30)",
            maxLength: 30,
            nullable: true)
            .Annotation("MySql:CharSet", "utf8mb4");
    }
}
