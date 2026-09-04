using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DespensaApp.Migrations
{
    /// <inheritdoc />
    public partial class AddVencimientoYCodigosAlternativos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "fecha_vencimiento",
                table: "productos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "codigos_barra_alternativos",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    producto_id = table.Column<int>(type: "integer", nullable: false),
                    codigo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_codigos_barra_alternativos", x => x.id);
                    table.ForeignKey(
                        name: "FK_codigos_barra_alternativos_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_codigos_barra_alternativos_codigo",
                table: "codigos_barra_alternativos",
                column: "codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_codigos_barra_alternativos_producto_id",
                table: "codigos_barra_alternativos",
                column: "producto_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "codigos_barra_alternativos");

            migrationBuilder.DropColumn(
                name: "fecha_vencimiento",
                table: "productos");
        }
    }
}
