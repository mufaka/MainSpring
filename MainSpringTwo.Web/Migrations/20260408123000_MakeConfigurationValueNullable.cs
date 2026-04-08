using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MainSpringTwo.Web.Migrations
{
    /// <inheritdoc />
    public partial class MakeConfigurationValueNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "ConfigurationValues",
                type: "TEXT",
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 4000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Value",
                table: "ConfigurationValues",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: string.Empty,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 4000,
                oldNullable: true);
        }
    }
}
