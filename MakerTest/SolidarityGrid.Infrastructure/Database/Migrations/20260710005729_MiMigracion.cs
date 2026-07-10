using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SolidarityGrid.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class MiMigracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    TransactionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OwnerNodeId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    CompletedByNodeId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.TransactionId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Owner_Status",
                table: "Payments",
                columns: new[] { "OwnerNodeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Status_Lease",
                table: "Payments",
                columns: new[] { "Status", "LeaseUntil" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
