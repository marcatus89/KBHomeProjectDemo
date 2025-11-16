using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReplyToTicketComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReplyToCommentId",
                table: "TicketComments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketComments_ReplyToCommentId",
                table: "TicketComments",
                column: "ReplyToCommentId");

            migrationBuilder.AddForeignKey(
                name: "FK_TicketComments_TicketComments_ReplyToCommentId",
                table: "TicketComments",
                column: "ReplyToCommentId",
                principalTable: "TicketComments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TicketComments_TicketComments_ReplyToCommentId",
                table: "TicketComments");

            migrationBuilder.DropIndex(
                name: "IX_TicketComments_ReplyToCommentId",
                table: "TicketComments");

            migrationBuilder.DropColumn(
                name: "ReplyToCommentId",
                table: "TicketComments");
        }
    }
}
