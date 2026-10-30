using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpendingTracker.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerUserId",
                table: "Expenses",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_OwnerUserId",
                table: "Expenses",
                column: "OwnerUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_AspNetUsers_OwnerUserId",
                table: "Expenses",
                column: "OwnerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_AspNetUsers_OwnerUserId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_OwnerUserId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Expenses");
        }
    }
}
