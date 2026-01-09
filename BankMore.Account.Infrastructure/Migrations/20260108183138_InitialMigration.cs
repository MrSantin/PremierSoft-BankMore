using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankMore.Account.Infrastructure.Migrations
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
                name: "Usuarios",
                columns: table => new
                {
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Senha = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoLogin = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "contacorrente",
                columns: table => new
                {
                    idcontacorrente = table.Column<Guid>(type: "uniqueidentifier", maxLength: 37, nullable: false),
                    numero = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "100000, 1"),
                    nome = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    senha = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    salt = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdUsuario = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacorrente", x => x.idcontacorrente);
                    table.CheckConstraint("CK_Ativo_Boolean", "ativo IN (0, 1)");
                    table.ForeignKey(
                        name: "FK_contacorrente_Usuarios_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuarios",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "movimento",
                columns: table => new
                {
                    idmovimento = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    idcontacorrente = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    datamovimento = table.Column<DateTime>(type: "datetime", nullable: false),
                    tipomovimento = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false),
                    valor = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimento", x => x.idmovimento);
                    table.CheckConstraint("CK_TipoMovimento", "tipomovimento IN ('C', 'D')");
                    table.ForeignKey(
                        name: "FK_movimento_contacorrente_idcontacorrente",
                        column: x => x.idcontacorrente,
                        principalTable: "contacorrente",
                        principalColumn: "idcontacorrente",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contacorrente_IdUsuario",
                table: "contacorrente",
                column: "IdUsuario",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_contacorrente_numero",
                table: "contacorrente",
                column: "numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_movimento_idcontacorrente",
                table: "movimento",
                column: "idcontacorrente");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Cpf",
                table: "Usuarios",
                column: "Cpf",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotencia");

            migrationBuilder.DropTable(
                name: "movimento");

            migrationBuilder.DropTable(
                name: "contacorrente");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
