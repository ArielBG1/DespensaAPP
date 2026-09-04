using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DespensaApp.Migrations
{
    /// <inheritdoc />
    public partial class GuardarImagenesEnBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "producto_imagenes",
                columns: table => new
                {
                    producto_id = table.Column<int>(type: "integer", nullable: false),
                    datos = table.Column<byte[]>(type: "bytea", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_producto_imagenes", x => x.producto_id);
                    table.ForeignKey(
                        name: "FK_producto_imagenes_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Las fotos anteriores vivían en wwwroot/uploads y no se pueden recuperar desde la base.
            migrationBuilder.Sql("UPDATE productos SET imagen_url = NULL WHERE imagen_url LIKE '/uploads/%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "producto_imagenes");
        }
    }
}
