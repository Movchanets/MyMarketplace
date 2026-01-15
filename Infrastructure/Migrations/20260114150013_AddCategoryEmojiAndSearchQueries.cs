using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryEmojiAndSearchQueries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Emoji",
                table: "Categories",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SearchQueries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Query = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NormalizedQuery = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SearchCount = table.Column<long>(type: "bigint", nullable: false),
                    LastSearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchQueries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_LastSearchedAt",
                table: "SearchQueries",
                column: "LastSearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_NormalizedQuery",
                table: "SearchQueries",
                column: "NormalizedQuery",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueries_SearchCount",
                table: "SearchQueries",
                column: "SearchCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SearchQueries");

            migrationBuilder.DropColumn(
                name: "Emoji",
                table: "Categories");
        }
    }
}
