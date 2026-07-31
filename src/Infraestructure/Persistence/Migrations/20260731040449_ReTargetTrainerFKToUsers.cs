using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReTargetTrainerFKToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymClasses_Trainers_TrainerId",
                table: "GymClasses");

            migrationBuilder.DropForeignKey(
                name: "FK_GymClassSchedules_Trainers_TrainerId",
                table: "GymClassSchedules");

            migrationBuilder.AddForeignKey(
                name: "FK_GymClasses_User_TrainerId",
                table: "GymClasses",
                column: "TrainerId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GymClassSchedules_User_TrainerId",
                table: "GymClassSchedules",
                column: "TrainerId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GymClasses_User_TrainerId",
                table: "GymClasses");

            migrationBuilder.DropForeignKey(
                name: "FK_GymClassSchedules_User_TrainerId",
                table: "GymClassSchedules");

            migrationBuilder.AddForeignKey(
                name: "FK_GymClasses_Trainers_TrainerId",
                table: "GymClasses",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GymClassSchedules_Trainers_TrainerId",
                table: "GymClassSchedules",
                column: "TrainerId",
                principalTable: "Trainers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
