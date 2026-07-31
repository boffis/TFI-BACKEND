using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GymManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestructureRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop old FKs and indexes first
            migrationBuilder.DropForeignKey(
                name: "FK_Inscriptions_Clients_ClientId",
                table: "Inscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Memberships_Clients_ClientId",
                table: "Memberships");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Memberships_MembershipId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Memberships_ClientId",
                table: "Memberships");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Inscriptions",
                table: "Inscriptions");

            migrationBuilder.DropColumn(
                name: "ClassDate",
                table: "Inscriptions");

            // 1. Create User root table FIRST — before dropping any shared columns
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    DNI = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsUserDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    EmailConfirmationToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmationTokenExpiration = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordResetToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PasswordResetTokenExpiration = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.UserId);
                });

            // 2. Copy existing data into User before dropping columns
            migrationBuilder.Sql(@"
                INSERT INTO [User] (UserId, Name, Email, Password, DateOfBirth, DNI, Gender, PhoneNumber,
                                    IsUserDeleted, IsEmailConfirmed, EmailConfirmationToken,
                                    EmailConfirmationTokenExpiration, PasswordResetToken, PasswordResetTokenExpiration)
                SELECT UserId, Name, Email, Password, DateOfBirth, DNI, Gender, PhoneNumber,
                       IsUserDeleted, IsEmailConfirmed, EmailConfirmationToken,
                       EmailConfirmationTokenExpiration, PasswordResetToken, PasswordResetTokenExpiration
                FROM [Admins]
            ");

            migrationBuilder.Sql(@"
                INSERT INTO [User] (UserId, Name, Email, Password, DateOfBirth, DNI, Gender, PhoneNumber,
                                    IsUserDeleted, IsEmailConfirmed, EmailConfirmationToken,
                                    EmailConfirmationTokenExpiration, PasswordResetToken, PasswordResetTokenExpiration)
                SELECT UserId, Name, Email, Password, DateOfBirth, DNI, Gender, PhoneNumber,
                       IsUserDeleted, IsEmailConfirmed, EmailConfirmationToken,
                       EmailConfirmationTokenExpiration, PasswordResetToken, PasswordResetTokenExpiration
                FROM [Clients]
            ");

            migrationBuilder.Sql(@"
                INSERT INTO [User] (UserId, Name, Email, Password, DateOfBirth, DNI, Gender, PhoneNumber,
                                    IsUserDeleted, IsEmailConfirmed, EmailConfirmationToken,
                                    EmailConfirmationTokenExpiration, PasswordResetToken, PasswordResetTokenExpiration)
                SELECT UserId, Name, Email, Password, DateOfBirth, DNI, Gender, PhoneNumber,
                       IsUserDeleted, IsEmailConfirmed, EmailConfirmationToken,
                       EmailConfirmationTokenExpiration, PasswordResetToken, PasswordResetTokenExpiration
                FROM [Trainers]
            ");

            // 3. Drop shared columns that moved to User
            migrationBuilder.DropColumn(name: "DNI", table: "Trainers");
            migrationBuilder.DropColumn(name: "DateOfBirth", table: "Trainers");
            migrationBuilder.DropColumn(name: "Email", table: "Trainers");
            migrationBuilder.DropColumn(name: "EmailConfirmationToken", table: "Trainers");
            migrationBuilder.DropColumn(name: "EmailConfirmationTokenExpiration", table: "Trainers");
            migrationBuilder.DropColumn(name: "Gender", table: "Trainers");
            migrationBuilder.DropColumn(name: "IsEmailConfirmed", table: "Trainers");
            migrationBuilder.DropColumn(name: "IsUserDeleted", table: "Trainers");
            migrationBuilder.DropColumn(name: "Name", table: "Trainers");
            migrationBuilder.DropColumn(name: "Password", table: "Trainers");
            migrationBuilder.DropColumn(name: "PasswordResetToken", table: "Trainers");
            migrationBuilder.DropColumn(name: "PasswordResetTokenExpiration", table: "Trainers");
            migrationBuilder.DropColumn(name: "PhoneNumber", table: "Trainers");

            migrationBuilder.DropColumn(name: "DNI", table: "Clients");
            migrationBuilder.DropColumn(name: "DateOfBirth", table: "Clients");
            migrationBuilder.DropColumn(name: "Email", table: "Clients");
            migrationBuilder.DropColumn(name: "EmailConfirmationToken", table: "Clients");
            migrationBuilder.DropColumn(name: "EmailConfirmationTokenExpiration", table: "Clients");
            migrationBuilder.DropColumn(name: "Gender", table: "Clients");
            migrationBuilder.DropColumn(name: "IsEmailConfirmed", table: "Clients");
            migrationBuilder.DropColumn(name: "IsUserDeleted", table: "Clients");
            migrationBuilder.DropColumn(name: "Name", table: "Clients");
            migrationBuilder.DropColumn(name: "Password", table: "Clients");
            migrationBuilder.DropColumn(name: "PasswordResetToken", table: "Clients");
            migrationBuilder.DropColumn(name: "PasswordResetTokenExpiration", table: "Clients");
            migrationBuilder.DropColumn(name: "PhoneNumber", table: "Clients");

            migrationBuilder.DropColumn(name: "DNI", table: "Admins");
            migrationBuilder.DropColumn(name: "DateOfBirth", table: "Admins");
            migrationBuilder.DropColumn(name: "Email", table: "Admins");
            migrationBuilder.DropColumn(name: "EmailConfirmationToken", table: "Admins");
            migrationBuilder.DropColumn(name: "EmailConfirmationTokenExpiration", table: "Admins");
            migrationBuilder.DropColumn(name: "Gender", table: "Admins");
            migrationBuilder.DropColumn(name: "IsEmailConfirmed", table: "Admins");
            migrationBuilder.DropColumn(name: "IsUserDeleted", table: "Admins");
            migrationBuilder.DropColumn(name: "Name", table: "Admins");
            migrationBuilder.DropColumn(name: "Password", table: "Admins");
            migrationBuilder.DropColumn(name: "PasswordResetToken", table: "Admins");
            migrationBuilder.DropColumn(name: "PasswordResetTokenExpiration", table: "Admins");
            migrationBuilder.DropColumn(name: "PhoneNumber", table: "Admins");

            // 4. Rename Memberships.ClientId → UserId
            migrationBuilder.RenameColumn(
                name: "ClientId",
                table: "Memberships",
                newName: "UserId");

            // 5. Add new columns
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "Inscriptions",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "InscriptionId",
                table: "Inscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // 6. Backfill Payments.UserId via Memberships
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.UserId = m.UserId
                FROM [Payments] p
                INNER JOIN [Memberships] m ON p.MembershipId = m.MembershipId
            ");

            // 7. Populate InscriptionId for existing rows
            migrationBuilder.Sql(@"
                UPDATE [Inscriptions] SET [InscriptionId] = NEWID()
            ");

            // 8. New PK and indexes
            migrationBuilder.AddPrimaryKey(
                name: "PK_Inscriptions",
                table: "Inscriptions",
                column: "InscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_UserId",
                table: "Payments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_UserId",
                table: "Memberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Inscriptions_ClientId_GymClassId",
                table: "Inscriptions",
                columns: new[] { "ClientId", "GymClassId" },
                unique: true,
                filter: "[ClientId] IS NOT NULL");

            // 9. FK constraints last — User table is populated
            migrationBuilder.AddForeignKey(
                name: "FK_Admins_User_UserId",
                table: "Admins",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Clients_User_UserId",
                table: "Clients",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inscriptions_Clients_ClientId",
                table: "Inscriptions",
                column: "ClientId",
                principalTable: "Clients",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Memberships_User_UserId",
                table: "Memberships",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Memberships_MembershipId",
                table: "Payments",
                column: "MembershipId",
                principalTable: "Memberships",
                principalColumn: "MembershipId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_User_UserId",
                table: "Payments",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Trainers_User_UserId",
                table: "Trainers",
                column: "UserId",
                principalTable: "User",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Admins_User_UserId", table: "Admins");
            migrationBuilder.DropForeignKey(name: "FK_Clients_User_UserId", table: "Clients");
            migrationBuilder.DropForeignKey(name: "FK_Inscriptions_Clients_ClientId", table: "Inscriptions");
            migrationBuilder.DropForeignKey(name: "FK_Memberships_User_UserId", table: "Memberships");
            migrationBuilder.DropForeignKey(name: "FK_Payments_Memberships_MembershipId", table: "Payments");
            migrationBuilder.DropForeignKey(name: "FK_Payments_User_UserId", table: "Payments");
            migrationBuilder.DropForeignKey(name: "FK_Trainers_User_UserId", table: "Trainers");

            migrationBuilder.DropTable(name: "User");

            migrationBuilder.DropIndex(name: "IX_Payments_UserId", table: "Payments");
            migrationBuilder.DropIndex(name: "IX_Memberships_UserId", table: "Memberships");
            migrationBuilder.DropPrimaryKey(name: "PK_Inscriptions", table: "Inscriptions");
            migrationBuilder.DropIndex(name: "IX_Inscriptions_ClientId_GymClassId", table: "Inscriptions");
            migrationBuilder.DropColumn(name: "UserId", table: "Payments");
            migrationBuilder.DropColumn(name: "InscriptionId", table: "Inscriptions");

            migrationBuilder.RenameColumn(name: "UserId", table: "Memberships", newName: "ClientId");

            migrationBuilder.AlterColumn<Guid>(
                name: "ClientId",
                table: "Inscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClassDate",
                table: "Inscriptions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(name: "DNI", table: "Trainers", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateOnly>(name: "DateOfBirth", table: "Trainers", type: "date", nullable: false, defaultValue: new DateOnly(1, 1, 1));
            migrationBuilder.AddColumn<string>(name: "Email", table: "Trainers", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "EmailConfirmationToken", table: "Trainers", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "EmailConfirmationTokenExpiration", table: "Trainers", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Gender", table: "Trainers", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "IsEmailConfirmed", table: "Trainers", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "IsUserDeleted", table: "Trainers", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "Name", table: "Trainers", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Password", table: "Trainers", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PasswordResetToken", table: "Trainers", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PasswordResetTokenExpiration", table: "Trainers", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "PhoneNumber", table: "Trainers", type: "nvarchar(max)", nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(name: "DNI", table: "Clients", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateOnly>(name: "DateOfBirth", table: "Clients", type: "date", nullable: false, defaultValue: new DateOnly(1, 1, 1));
            migrationBuilder.AddColumn<string>(name: "Email", table: "Clients", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "EmailConfirmationToken", table: "Clients", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "EmailConfirmationTokenExpiration", table: "Clients", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Gender", table: "Clients", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "IsEmailConfirmed", table: "Clients", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "IsUserDeleted", table: "Clients", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "Name", table: "Clients", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Password", table: "Clients", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PasswordResetToken", table: "Clients", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PasswordResetTokenExpiration", table: "Clients", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "PhoneNumber", table: "Clients", type: "nvarchar(max)", nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<string>(name: "DNI", table: "Admins", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<DateOnly>(name: "DateOfBirth", table: "Admins", type: "date", nullable: false, defaultValue: new DateOnly(1, 1, 1));
            migrationBuilder.AddColumn<string>(name: "Email", table: "Admins", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "EmailConfirmationToken", table: "Admins", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "EmailConfirmationTokenExpiration", table: "Admins", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Gender", table: "Admins", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<bool>(name: "IsEmailConfirmed", table: "Admins", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<bool>(name: "IsUserDeleted", table: "Admins", type: "bit", nullable: false, defaultValue: false);
            migrationBuilder.AddColumn<string>(name: "Name", table: "Admins", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "Password", table: "Admins", type: "nvarchar(max)", nullable: false, defaultValue: "");
            migrationBuilder.AddColumn<string>(name: "PasswordResetToken", table: "Admins", type: "nvarchar(max)", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "PasswordResetTokenExpiration", table: "Admins", type: "datetime2", nullable: true);
            migrationBuilder.AddColumn<string>(name: "PhoneNumber", table: "Admins", type: "nvarchar(max)", nullable: false, defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Inscriptions",
                table: "Inscriptions",
                columns: new[] { "ClientId", "GymClassId" });

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ClientId",
                table: "Memberships",
                column: "ClientId",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Memberships_MembershipId",
                table: "Payments",
                column: "MembershipId",
                principalTable: "Memberships",
                principalColumn: "MembershipId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
