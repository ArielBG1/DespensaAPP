using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DespensaApp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMonedaAGuaranies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UPSERT: en instalaciones ya en marcha, "MonedaNombre" puede existir con valor "Peso"
            // (insertado por SeedData.cs antes de que se agregara al seed de EF), por eso se usa
            // ON CONFLICT en vez de InsertData/UpdateData directos.
            migrationBuilder.Sql(@"
                INSERT INTO configuraciones (clave, valor, descripcion, updated_at)
                VALUES ('MonedaSimbolo', 'Gs.', 'Símbolo de moneda', NOW())
                ON CONFLICT (clave) DO UPDATE SET valor = 'Gs.', updated_at = NOW();

                INSERT INTO configuraciones (clave, valor, descripcion, updated_at)
                VALUES ('MonedaNombre', 'Guaraní', 'Nombre de la moneda', NOW())
                ON CONFLICT (clave) DO UPDATE SET valor = 'Guaraní', updated_at = NOW();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE configuraciones SET valor = '$', updated_at = NOW() WHERE clave = 'MonedaSimbolo';
                UPDATE configuraciones SET valor = 'Peso', updated_at = NOW() WHERE clave = 'MonedaNombre';
            ");
        }
    }
}
