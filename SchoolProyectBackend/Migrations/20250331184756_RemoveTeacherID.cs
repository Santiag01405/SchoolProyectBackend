using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolProyectBackend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTeacherID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_courses_teacher_TeacherID",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_parent_ParentID",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_teacher_TeacherID",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_teacher_user_UserID",
                table: "teacher");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_ParentID",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TeacherID",
                table: "Notifications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_teacher",
                table: "teacher");

            migrationBuilder.DropColumn(
                name: "ParentID",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TeacherID",
                table: "Notifications");

            migrationBuilder.RenameTable(
                name: "teacher",
                newName: "Teachers");

            migrationBuilder.RenameIndex(
                name: "IX_teacher_UserID",
                table: "Teachers",
                newName: "IX_Teachers_UserID");

            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "TeacherID",
                table: "courses",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "UserID",
                table: "courses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers",
                column: "TeacherID");

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    AttendanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    RelatedUserID = table.Column<int>(type: "int", nullable: false),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => x.AttendanceID);
                });

            migrationBuilder.CreateTable(
                name: "Evaluations",
                columns: table => new
                {
                    EvaluationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Evaluations", x => x.EvaluationID);
                    table.ForeignKey(
                        name: "FK_Evaluations_courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Evaluations_user_UserID",
                        column: x => x.UserID,
                        principalTable: "user",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRelationships",
                columns: table => new
                {
                    RelationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    User1ID = table.Column<int>(type: "int", nullable: false),
                    User2ID = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRelationships", x => x.RelationID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_courses_UserID",
                table: "courses",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_CourseID",
                table: "Evaluations",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_Evaluations_UserID",
                table: "Evaluations",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_UserRelationships_User1ID_User2ID_RelationshipType",
                table: "UserRelationships",
                columns: new[] { "User1ID", "User2ID", "RelationshipType" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_courses_Teachers_TeacherID",
                table: "courses",
                column: "TeacherID",
                principalTable: "Teachers",
                principalColumn: "TeacherID");

            migrationBuilder.AddForeignKey(
                name: "FK_courses_user_UserID",
                table: "courses",
                column: "UserID",
                principalTable: "user",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_user_UserID",
                table: "Notifications",
                column: "UserID",
                principalTable: "user",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teachers_user_UserID",
                table: "Teachers",
                column: "UserID",
                principalTable: "user",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_courses_Teachers_TeacherID",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_courses_user_UserID",
                table: "courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_user_UserID",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Teachers_user_UserID",
                table: "Teachers");

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "Evaluations");

            migrationBuilder.DropTable(
                name: "UserRelationships");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserID",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_courses_UserID",
                table: "courses");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Teachers",
                table: "Teachers");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "UserID",
                table: "courses");

            migrationBuilder.RenameTable(
                name: "Teachers",
                newName: "teacher");

            migrationBuilder.RenameIndex(
                name: "IX_Teachers_UserID",
                table: "teacher",
                newName: "IX_teacher_UserID");

            migrationBuilder.AddColumn<int>(
                name: "ParentID",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherID",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "TeacherID",
                table: "courses",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_teacher",
                table: "teacher",
                column: "TeacherID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ParentID",
                table: "Notifications",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TeacherID",
                table: "Notifications",
                column: "TeacherID");

            migrationBuilder.AddForeignKey(
                name: "FK_courses_teacher_TeacherID",
                table: "courses",
                column: "TeacherID",
                principalTable: "teacher",
                principalColumn: "TeacherID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_parent_ParentID",
                table: "Notifications",
                column: "ParentID",
                principalTable: "parent",
                principalColumn: "ParentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_teacher_TeacherID",
                table: "Notifications",
                column: "TeacherID",
                principalTable: "teacher",
                principalColumn: "TeacherID");

            migrationBuilder.AddForeignKey(
                name: "FK_teacher_user_UserID",
                table: "teacher",
                column: "UserID",
                principalTable: "user",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
