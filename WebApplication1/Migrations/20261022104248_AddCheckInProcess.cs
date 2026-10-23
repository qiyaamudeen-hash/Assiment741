using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication1.Migrations
{
   
    public partial class AddCheckInProcess : Migration
    {
      
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CheckInProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyId = table.Column<int>(type: "int", nullable: false),
                    CheckInTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    CheckOutTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    HouseRules = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CheckInInstructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RequiredDocuments = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CheckInProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CheckInProcesses_HostProperties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "HostProperties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CheckInProcesses_PropertyId",
                table: "CheckInProcesses",
                column: "PropertyId",
                unique: true);
        }

        
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CheckInProcesses");
        }
    }
}
