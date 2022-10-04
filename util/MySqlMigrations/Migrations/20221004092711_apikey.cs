﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bit.MySqlMigrations.Migrations;

public partial class apikey : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ApiKey",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                UserId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                OrganizationId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                ServiceAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                ClientSecret = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                Scope = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                EncryptedPayload = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                    .Annotation("MySql:CharSet", "utf8mb4"),
                CreationDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                RevisionDate = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
            })
            .Annotation("MySql:CharSet", "utf8mb4");

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
    }
}
