using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgentFw.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAiProviders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ai_providers",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    provider_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    auth_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    protected_api_key = table.Column<string>(type: "text", nullable: true),
                    api_key_hint = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    chat_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    embedding_model = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    embedding_dimensions = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_providers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_providers_is_active",
                table: "ai_providers",
                column: "is_active",
                unique: true,
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_ai_providers_name",
                table: "ai_providers",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ai_providers");
        }
    }
}
