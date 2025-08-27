using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Super_Sonic.Migrations
{
    /// <inheritdoc />
    public partial class addLogForPartnerCapital : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PartnerLogForInvest_Drawal",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsDebit = table.Column<bool>(type: "bit", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PartnerId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PartnerLogForInvest_Drawal", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PartnerLogForInvest_Drawal_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "NationalId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PartnerLogForInvest_Drawal_PartnerId",
                table: "PartnerLogForInvest_Drawal",
                column: "PartnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PartnerLogForInvest_Drawal");
        }
    }
}
