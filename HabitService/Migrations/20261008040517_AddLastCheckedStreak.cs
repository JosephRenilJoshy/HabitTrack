using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HabitService.Migrations
{
    /// <inheritdoc />
    public partial class AddLastCheckedStreak : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastCheckedStreak",
                table: "Habits",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastCheckedStreak",
                table: "Habits");
        }
    }
}
