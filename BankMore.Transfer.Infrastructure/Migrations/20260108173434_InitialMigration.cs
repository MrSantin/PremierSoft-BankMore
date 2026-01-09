using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankMore.Transfer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotencia",
                columns: table => new
                {
                    chave_idempotencia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    requisicao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    resultado = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotencia", x => x.chave_idempotencia);
                });

            migrationBuilder.CreateTable(
                name: "transferencia",
                columns: table => new
                {
                    idtransferencia = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idcontacorrente_origem = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idcontacorrente_destino = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    datamovimento = table.Column<DateTime>(type: "datetime", nullable: false),
                    valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transferencia", x => x.idtransferencia);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotencia");

            migrationBuilder.DropTable(
                name: "transferencia");
        }
    }
}
