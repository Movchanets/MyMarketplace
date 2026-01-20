using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSkuAttributeValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkuAttributeValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkuId = table.Column<Guid>(type: "uuid", nullable: false),
                    AttributeDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ValueString = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ValueNumber = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ValueBoolean = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkuAttributeValues", x => x.Id);
                    table.CheckConstraint("CK_SkuAttributeValue_OnlyOneValueType", "(CASE WHEN \"ValueString\" IS NOT NULL THEN 1 ELSE 0 END +\n				  CASE WHEN \"ValueNumber\" IS NOT NULL THEN 1 ELSE 0 END +\n				  CASE WHEN \"ValueBoolean\" IS NOT NULL THEN 1 ELSE 0 END) = 1");
                    table.ForeignKey(
                        name: "FK_SkuAttributeValues_AttributeDefinitions_AttributeDefinition~",
                        column: x => x.AttributeDefinitionId,
                        principalTable: "AttributeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SkuAttributeValues_Skus_SkuId",
                        column: x => x.SkuId,
                        principalTable: "Skus",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkuAttributeValues_AttributeDefinitionId",
                table: "SkuAttributeValues",
                column: "AttributeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_SkuAttributeValues_AttributeId_ValueBoolean",
                table: "SkuAttributeValues",
                columns: new[] { "AttributeDefinitionId", "ValueBoolean" });

            migrationBuilder.CreateIndex(
                name: "IX_SkuAttributeValues_AttributeId_ValueNumber",
                table: "SkuAttributeValues",
                columns: new[] { "AttributeDefinitionId", "ValueNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_SkuAttributeValues_AttributeId_ValueString",
                table: "SkuAttributeValues",
                columns: new[] { "AttributeDefinitionId", "ValueString" });

            migrationBuilder.CreateIndex(
                name: "IX_SkuAttributeValues_SkuId",
                table: "SkuAttributeValues",
                column: "SkuId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkuAttributeValues");
        }
    }
}
