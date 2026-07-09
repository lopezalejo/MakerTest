using Microsoft.EntityFrameworkCore.Migrations;

namespace SolidarityGrid.Infrastructure.Database.Migrations
{
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    TransactionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<byte>(type: "tinyint", nullable: false),
                    OwnerNodeId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LeaseUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    CompletedByNodeId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResultMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
                columns: new[] { "Status", "LeaseUntilUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Payments");
        }
    }
}
