using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PortfolioCms.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMailAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutgoingMailId = table.Column<int>(type: "int", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    StoredName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailAttachments_OutgoingMails_OutgoingMailId",
                        column: x => x.OutgoingMailId,
                        principalTable: "OutgoingMails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailAttachments_OutgoingMailId",
                table: "MailAttachments",
                column: "OutgoingMailId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailAttachments");
        }
    }
}
