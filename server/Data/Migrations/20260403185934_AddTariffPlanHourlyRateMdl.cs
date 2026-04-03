using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberServer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTariffPlanHourlyRateMdl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "TariffPlans",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRateMdl",
                table: "TariffPlans",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_TariffPlans_ClubId",
                table: "TariffPlans",
                column: "ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_TariffPlans_Clubs_ClubId",
                table: "TariffPlans",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TariffPlans_Clubs_ClubId",
                table: "TariffPlans");

            migrationBuilder.DropIndex(
                name: "IX_TariffPlans_ClubId",
                table: "TariffPlans");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "TariffPlans");

            migrationBuilder.DropColumn(
                name: "HourlyRateMdl",
                table: "TariffPlans");
        }
    }
}
