using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Super_Sonic.Migrations
{
    /// <inheritdoc />
    public partial class modifyLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "PartnerLogForInvest_Drawals",
                type: "decimal(15,5)",
                nullable: false,
                defaultValue: 0m);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
      

            migrationBuilder.DropColumn(
                name: "Amount",
                table: "PartnerLogForInvest_Drawals");
        }
    }
}
