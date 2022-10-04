﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.PostgresMigrations.Migrations;

public partial class apikey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<DateTime>(
            name: "ResponseDate",
            table: "AuthRequest",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "RequestIpAddress",
            table: "AuthRequest",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "RequestDeviceIdentifier",
            table: "AuthRequest",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreationDate",
            table: "AuthRequest",
            type: "timestamp without time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "AuthenticationDate",
            table: "AuthRequest",
            type: "timestamp without time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp with time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AccessCode",
            table: "AuthRequest",
            type: "character varying(25)",
            maxLength: 25,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        migrationBuilder.CreateTable(
            name: "ApiKey",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: true),
                OrganizationId = table.Column<Guid>(type: "uuid", nullable: true),
                ServiceAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                ClientSecret = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                Scope = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                EncryptedPayload = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                CreationDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                RevisionDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApiKey", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApiKey_Organization_OrganizationId",
                    column: x => x.OrganizationId,
                    principalTable: "Organization",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ApiKey_ServiceAccount_ServiceAccountId",
                    column: x => x.ServiceAccountId,
                    principalTable: "ServiceAccount",
                    principalColumn: "Id");
                table.ForeignKey(
                    name: "FK_ApiKey_User_UserId",
                    column: x => x.UserId,
                    principalTable: "User",
                    principalColumn: "Id");
            });

        migrationBuilder.CreateIndex(
            name: "IX_ApiKey_OrganizationId",
            table: "ApiKey",
            column: "OrganizationId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiKey_ServiceAccountId",
            table: "ApiKey",
            column: "ServiceAccountId");

        migrationBuilder.CreateIndex(
            name: "IX_ApiKey_UserId",
            table: "ApiKey",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ApiKey");

        migrationBuilder.AlterColumn<DateTime>(
            name: "ResponseDate",
            table: "AuthRequest",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "RequestIpAddress",
            table: "AuthRequest",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50,
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "RequestDeviceIdentifier",
            table: "AuthRequest",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(50)",
            oldMaxLength: 50,
            oldNullable: true);

        migrationBuilder.AlterColumn<DateTime>(
            name: "CreationDate",
            table: "AuthRequest",
            type: "timestamp with time zone",
            nullable: false,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone");

        migrationBuilder.AlterColumn<DateTime>(
            name: "AuthenticationDate",
            table: "AuthRequest",
            type: "timestamp with time zone",
            nullable: true,
            oldClrType: typeof(DateTime),
            oldType: "timestamp without time zone",
            oldNullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "AccessCode",
            table: "AuthRequest",
            type: "text",
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(25)",
            oldMaxLength: 25,
            oldNullable: true);
    }
}
