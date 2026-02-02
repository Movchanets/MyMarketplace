using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SetRowVersionDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure pgcrypto extension is available for gen_random_bytes
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pgcrypto;");
            // Set default generation for rowversion-like columns using pgcrypto
            migrationBuilder.Sql("ALTER TABLE \"Carts\" ALTER COLUMN \"RowVersion\" SET DEFAULT gen_random_bytes(8);");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" ALTER COLUMN \"RowVersion\" SET DEFAULT gen_random_bytes(8);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Carts\" ALTER COLUMN \"RowVersion\" DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE \"Orders\" ALTER COLUMN \"RowVersion\" DROP DEFAULT;");
        }
    }
}
