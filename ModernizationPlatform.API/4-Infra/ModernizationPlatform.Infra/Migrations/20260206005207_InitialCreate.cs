using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernizationPlatform.Infra.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analysis_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    repository_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    selected_types = table.Column<string>(type: "jsonb", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "prompts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prompts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "repositories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_analysis_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repositories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "analysis_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    output = table.Column<string>(type: "jsonb", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_jobs", x => x.id);
                    table.ForeignKey(
                        name: "FK_analysis_jobs_analysis_requests",
                        column: x => x.request_id,
                        principalTable: "analysis_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shared_contexts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    languages = table.Column<string>(type: "jsonb", nullable: false),
                    frameworks = table.Column<string>(type: "jsonb", nullable: false),
                    dependencies = table.Column<string>(type: "jsonb", nullable: false),
                    directory_structure = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shared_contexts", x => x.id);
                    table.ForeignKey(
                        name: "FK_shared_contexts_analysis_requests",
                        column: x => x.request_id,
                        principalTable: "analysis_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "findings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    severity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_findings", x => x.id);
                    table.ForeignKey(
                        name: "FK_findings_analysis_jobs",
                        column: x => x.job_id,
                        principalTable: "analysis_jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_jobs_request_id",
                table: "analysis_jobs",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_jobs_status",
                table: "analysis_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_requests_created_at",
                table: "analysis_requests",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_requests_status",
                table: "analysis_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_findings_job_id",
                table: "findings",
                column: "job_id");

            migrationBuilder.CreateIndex(
                name: "IX_findings_severity",
                table: "findings",
                column: "severity");

            migrationBuilder.CreateIndex(
                name: "IX_prompts_analysis_type",
                table: "prompts",
                column: "analysis_type",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_repositories_url",
                table: "repositories",
                column: "url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shared_contexts_request_id",
                table: "shared_contexts",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "findings");

            migrationBuilder.DropTable(
                name: "prompts");

            migrationBuilder.DropTable(
                name: "repositories");

            migrationBuilder.DropTable(
                name: "shared_contexts");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "analysis_jobs");

            migrationBuilder.DropTable(
                name: "analysis_requests");
        }
    }
}
