using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainSpringTwo.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddNextRunTimeToScheduledJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "NextRunTime",
                table: "ScheduledJobs",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextRunTime",
                table: "ScheduledJobs");
        }
    }
}
