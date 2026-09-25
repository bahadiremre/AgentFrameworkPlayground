using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgentFw.Data.Migrations
{
    /// <inheritdoc />
    public partial class SnakeCaseNaming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_todos",
                table: "todos");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "todos",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "todos",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "IsCompleted",
                table: "todos",
                newName: "is_completed");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "todos",
                newName: "created_at");

            migrationBuilder.AddPrimaryKey(
                name: "pk_todos",
                table: "todos",
                column: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_todos",
                table: "todos");

            migrationBuilder.RenameColumn(
                name: "title",
                table: "todos",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "todos",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "is_completed",
                table: "todos",
                newName: "IsCompleted");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "todos",
                newName: "CreatedAt");

            migrationBuilder.AddPrimaryKey(
                name: "PK_todos",
                table: "todos",
                column: "Id");
        }
    }
}
