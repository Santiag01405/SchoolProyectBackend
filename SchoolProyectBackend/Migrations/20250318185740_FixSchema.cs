using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolProyectBackend.Migrations
{
    /// <inheritdoc />
    public partial class FixSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    RoleID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleID);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user", x => x.UserID);
                    table.ForeignKey(
                        name: "FK_user_Roles_RoleID",
                        column: x => x.RoleID,
                        principalTable: "Roles",
                        principalColumn: "RoleID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "parent",
                columns: table => new
                {
                    ParentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parent", x => x.ParentID);
                    table.ForeignKey(
                        name: "FK_parent_user_UserID",
                        column: x => x.UserID,
                        principalTable: "user",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "teacher",
                columns: table => new
                {
                    TeacherID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teacher", x => x.TeacherID);
                    table.ForeignKey(
                        name: "FK_teacher_user_UserID",
                        column: x => x.UserID,
                        principalTable: "user",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "student",
                columns: table => new
                {
                    StudentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentID = table.Column<int>(type: "int", nullable: true),
                    UserID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student", x => x.StudentID);
                    table.ForeignKey(
                        name: "FK_student_parent_ParentID",
                        column: x => x.ParentID,
                        principalTable: "parent",
                        principalColumn: "ParentID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_user_UserID",
                        column: x => x.UserID,
                        principalTable: "user",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "courses",
                columns: table => new
                {
                    CourseID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TeacherID = table.Column<int>(type: "int", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_courses", x => x.CourseID);
                    table.ForeignKey(
                        name: "FK_courses_teacher_TeacherID",
                        column: x => x.TeacherID,
                        principalTable: "teacher",
                        principalColumn: "TeacherID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    notificationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: true),
                    TeacherID = table.Column<int>(type: "int", nullable: true),
                    ParentID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.notificationID);
                    table.ForeignKey(
                        name: "FK_Notifications_parent_ParentID",
                        column: x => x.ParentID,
                        principalTable: "parent",
                        principalColumn: "ParentID");
                    table.ForeignKey(
                        name: "FK_Notifications_student_StudentID",
                        column: x => x.StudentID,
                        principalTable: "student",
                        principalColumn: "StudentID");
                    table.ForeignKey(
                        name: "FK_Notifications_teacher_TeacherID",
                        column: x => x.TeacherID,
                        principalTable: "teacher",
                        principalColumn: "TeacherID");
                });

            migrationBuilder.CreateTable(
                name: "enrollment",
                columns: table => new
                {
                    EnrollmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    StudentID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_enrollment", x => x.EnrollmentID);
                    table.ForeignKey(
                        name: "FK_enrollment_courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_enrollment_student_StudentID",
                        column: x => x.StudentID,
                        principalTable: "student",
                        principalColumn: "StudentID");
                    table.ForeignKey(
                        name: "FK_enrollment_user_UserID",
                        column: x => x.UserID,
                        principalTable: "user",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "grades",
                columns: table => new
                {
                    GradeID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentID = table.Column<int>(type: "int", nullable: false),
                    CourseID = table.Column<int>(type: "int", nullable: false),
                    GradeValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grades", x => x.GradeID);
                    table.ForeignKey(
                        name: "FK_grades_courses_CourseID",
                        column: x => x.CourseID,
                        principalTable: "courses",
                        principalColumn: "CourseID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_grades_student_StudentID",
                        column: x => x.StudentID,
                        principalTable: "student",
                        principalColumn: "StudentID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifyRecip",
                columns: table => new
                {
                    recipientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    notificationID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifyRecip", x => x.recipientID);
                    table.ForeignKey(
                        name: "FK_notifyRecip_Notifications_notificationID",
                        column: x => x.notificationID,
                        principalTable: "Notifications",
                        principalColumn: "notificationID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_courses_TeacherID",
                table: "courses",
                column: "TeacherID");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_CourseID",
                table: "enrollment",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_StudentID",
                table: "enrollment",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_enrollment_UserID",
                table: "enrollment",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_grades_CourseID",
                table: "grades",
                column: "CourseID");

            migrationBuilder.CreateIndex(
                name: "IX_grades_StudentID",
                table: "grades",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ParentID",
                table: "Notifications",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StudentID",
                table: "Notifications",
                column: "StudentID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TeacherID",
                table: "Notifications",
                column: "TeacherID");

            migrationBuilder.CreateIndex(
                name: "IX_notifyRecip_notificationID",
                table: "notifyRecip",
                column: "notificationID");

            migrationBuilder.CreateIndex(
                name: "IX_parent_UserID",
                table: "parent",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_student_ParentID",
                table: "student",
                column: "ParentID");

            migrationBuilder.CreateIndex(
                name: "IX_student_UserID",
                table: "student",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_teacher_UserID",
                table: "teacher",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_user_RoleID",
                table: "user",
                column: "RoleID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enrollment");

            migrationBuilder.DropTable(
                name: "grades");

            migrationBuilder.DropTable(
                name: "notifyRecip");

            migrationBuilder.DropTable(
                name: "courses");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "student");

            migrationBuilder.DropTable(
                name: "teacher");

            migrationBuilder.DropTable(
                name: "parent");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
