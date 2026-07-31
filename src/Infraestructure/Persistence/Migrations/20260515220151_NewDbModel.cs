using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NewDbModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymClasses_Users_TrainerId",
                table: "GymClasses");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_Users_ClientId",
                table: "Inscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_Users_ClientId",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "Users");

            migrationBuilder.CreateTable(
                name: "Admins",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Admins", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Admins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Clients_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Trainers",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Specialization = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trainers", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_Trainers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_GymClasses_Trainers_TrainerId",
                table: "GymClasses",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_Clients_ClientId",
                table: "Inscriptions",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_Clients_ClientId",
                table: "Memberships",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymClasses_Trainers_TrainerId",
                table: "GymClasses");

            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_Clients_ClientId",
                table: "Inscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_Clients_ClientId",
                table: "Memberships");

            migrationBuilder.DropTable(
                name: "Admins");

            migrationBuilder.DropTable(
                name: "Clients");

            migrationBuilder.DropTable(
                name: "Trainers");

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GymClasses_Users_TrainerId",
                table: "GymClasses",
                column: "TrainerId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_Users_ClientId",
                table: "Inscriptions",
                column: "ClientId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_Users_ClientId",
                table: "Memberships",
                column: "ClientId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
