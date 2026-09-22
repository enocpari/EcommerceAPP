using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EcommerceApp.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create Categories table first
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Icon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            // 2. Insert default categories
            migrationBuilder.Sql(@"
                INSERT INTO ""Categories"" (""Name"", ""Description"", ""Icon"", ""DisplayOrder"", ""IsActive"", ""CreatedAt"")
                VALUES 
                ('Celulares', 'Smartphones y teléfonos móviles', '📱', 1, true, NOW()),
                ('Auriculares', 'Auriculares y audio de alta fidelidad', '🎧', 2, true, NOW()),
                ('Accesorios', 'Accesorios tecnológicos y complementos', '🔌', 3, true, NOW())
                ON CONFLICT (""Name"") DO NOTHING;
            ");

            // 3. Add new columns to Products (nullable)
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "Products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // 4. Migrate data from old Category string column to new CategoryId and CategoryName
            migrationBuilder.Sql(@"
                UPDATE ""Products"" p
                SET ""CategoryId"" = c.""Id"", ""CategoryName"" = c.""Name""
                FROM ""Categories"" c
                WHERE p.""Category"" IS NOT NULL 
                AND p.""Category"" <> ''
                AND c.""Name"" ILIKE p.""Category"";
            ");

            // 5. Also handle case where Category string might not exactly match (case-insensitive partial)
            migrationBuilder.Sql(@"
                UPDATE ""Products"" p
                SET ""CategoryId"" = c.""Id"", ""CategoryName"" = c.""Name""
                FROM ""Categories"" c
                WHERE p.""CategoryId"" IS NULL
                AND p.""Category"" IS NOT NULL 
                AND p.""Category"" <> ''
                AND c.""Name"" ILIKE '%' || p.""Category"" || '%';
            ");

            // 6. Now drop the old Category column
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Products");

            // 7. Add Orders columns
            migrationBuilder.AddColumn<string>(
                name: "AdminNotes",
                table: "Orders",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Orders",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            // 8. Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DocumentId",
                table: "Orders",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore old Category column first
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Products",
                type: "text",
                nullable: true);

            // Migrate data back from CategoryName to Category
            migrationBuilder.Sql(@"
                UPDATE ""Products"" 
                SET ""Category"" = ""CategoryName""
                WHERE ""CategoryName"" IS NOT NULL;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Categories_CategoryId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Products_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CreatedAt",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_DocumentId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AdminNotes",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Orders");
        }
    }
}
