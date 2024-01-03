﻿using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.MySqlMigrations.Migrations;

/// <inheritdoc />
public partial class Send_CipherId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_Grant",
            table: "Grant");

        migrationBuilder.UpdateData(
            table: "Grant",
            keyColumn: "Type",
            keyValue: null,
            column: "Type",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "Type",
            table: "Grant",
            type: "varchar(50)",
            maxLength: 50,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(50)",
            oldMaxLength: 50,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.UpdateData(
            table: "Grant",
            keyColumn: "Data",
            keyValue: null,
            column: "Data",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "Data",
            table: "Grant",
            type: "longtext",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "longtext",
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.UpdateData(
            table: "Grant",
            keyColumn: "ClientId",
            keyValue: null,
            column: "ClientId",
            value: "");

        migrationBuilder.AlterColumn<string>(
            name: "ClientId",
            table: "Grant",
            type: "varchar(200)",
            maxLength: 200,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(200)",
            oldMaxLength: 200,
            oldNullable: true)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddColumn<int>(
            name: "Id",
            table: "Grant",
            type: "int",
            nullable: false,
            defaultValue: 0)
            .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

        migrationBuilder.AddPrimaryKey(
            name: "PK_Grant",
            table: "Grant",
            column: "Id");

        migrationBuilder.CreateIndex(
            name: "IX_Grant_Key",
            table: "Grant",
            column: "Key",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropPrimaryKey(
            name: "PK_Grant",
            table: "Grant");

        migrationBuilder.DropIndex(
            name: "IX_Grant_Key",
            table: "Grant");

        migrationBuilder.DropColumn(
            name: "Id",
            table: "Grant");

        migrationBuilder.AlterColumn<string>(
            name: "Type",
            table: "Grant",
            type: "varchar(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(50)",
            oldMaxLength: 50)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "Data",
            table: "Grant",
            type: "longtext",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "longtext")
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AlterColumn<string>(
            name: "ClientId",
            table: "Grant",
            type: "varchar(200)",
            maxLength: 200,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "varchar(200)",
            oldMaxLength: 200)
            .Annotation("MySql:CharSet", "utf8mb4")
            .OldAnnotation("MySql:CharSet", "utf8mb4");

        migrationBuilder.AddPrimaryKey(
            name: "PK_Grant",
            table: "Grant",
            column: "Key");
    }
}
