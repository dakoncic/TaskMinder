using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTaskSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemTasks_Items_ItemId",
                table: "ItemTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemTasks",
                table: "ItemTasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Items",
                table: "Items");

            migrationBuilder.RenameTable(
                name: "ItemTasks",
                newName: "TaskOccurrences");

            migrationBuilder.RenameTable(
                name: "Items",
                newName: "TaskTemplates");

            migrationBuilder.RenameColumn(
                name: "ItemId",
                table: "TaskOccurrences",
                newName: "TaskTemplateId");

            migrationBuilder.RenameIndex(
                name: "IDX_ItemID",
                table: "TaskOccurrences",
                newName: "IDX_TaskTemplateID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskOccurrences",
                table: "TaskOccurrences",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaskTemplates",
                table: "TaskTemplates",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskOccurrences_TaskTemplates_TaskTemplateId",
                table: "TaskOccurrences",
                column: "TaskTemplateId",
                principalTable: "TaskTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskOccurrences_TaskTemplates_TaskTemplateId",
                table: "TaskOccurrences");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskTemplates",
                table: "TaskTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaskOccurrences",
                table: "TaskOccurrences");

            migrationBuilder.RenameTable(
                name: "TaskTemplates",
                newName: "Items");

            migrationBuilder.RenameTable(
                name: "TaskOccurrences",
                newName: "ItemTasks");

            migrationBuilder.RenameColumn(
                name: "TaskTemplateId",
                table: "ItemTasks",
                newName: "ItemId");

            migrationBuilder.RenameIndex(
                name: "IDX_TaskTemplateID",
                table: "ItemTasks",
                newName: "IDX_ItemID");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Items",
                table: "Items",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemTasks",
                table: "ItemTasks",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemTasks_Items_ItemId",
                table: "ItemTasks",
                column: "ItemId",
                principalTable: "Items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
