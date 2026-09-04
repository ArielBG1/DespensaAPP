using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DespensaApp.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexCodigoBarra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_productos_codigo_barra",
                table: "productos");

            migrationBuilder.CreateIndex(
                name: "IX_productos_codigo_barra",
                table: "productos",
                column: "codigo_barra",
                unique: true,
                filter: "codigo_barra IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_productos_codigo_barra",
                table: "productos");

            migrationBuilder.CreateIndex(
                name: "IX_productos_codigo_barra",
                table: "productos",
                column: "codigo_barra");
        }
    }
}
