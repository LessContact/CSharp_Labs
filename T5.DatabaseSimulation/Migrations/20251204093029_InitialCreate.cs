using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DatabaseSimulation.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SimulationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StrategyName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhilosopherCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SimulationRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForkStateLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationRunId = table.Column<int>(type: "integer", nullable: false),
                    ForkId = table.Column<int>(type: "integer", nullable: false),
                    TimestampMs = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    UsedByPhilosopherId = table.Column<int>(type: "integer", nullable: true),
                    UsedByPhilosopherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForkStateLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForkStateLogs_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhilosopherStateLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SimulationRunId = table.Column<int>(type: "integer", nullable: false),
                    PhilosopherId = table.Column<int>(type: "integer", nullable: false),
                    PhilosopherName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TimestampMs = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    HasLeftFork = table.Column<bool>(type: "boolean", nullable: false),
                    HasRightFork = table.Column<bool>(type: "boolean", nullable: false),
                    EatenCount = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhilosopherStateLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PhilosopherStateLogs_SimulationRuns_SimulationRunId",
                        column: x => x.SimulationRunId,
                        principalTable: "SimulationRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForkStateLogs_SimulationRunId_ForkId_TimestampMs",
                table: "ForkStateLogs",
                columns: new[] { "SimulationRunId", "ForkId", "TimestampMs" });

            migrationBuilder.CreateIndex(
                name: "IX_ForkStateLogs_SimulationRunId_TimestampMs",
                table: "ForkStateLogs",
                columns: new[] { "SimulationRunId", "TimestampMs" });

            migrationBuilder.CreateIndex(
                name: "IX_PhilosopherStateLogs_SimulationRunId_PhilosopherId_Timestam~",
                table: "PhilosopherStateLogs",
                columns: new[] { "SimulationRunId", "PhilosopherId", "TimestampMs" });

            migrationBuilder.CreateIndex(
                name: "IX_PhilosopherStateLogs_SimulationRunId_TimestampMs",
                table: "PhilosopherStateLogs",
                columns: new[] { "SimulationRunId", "TimestampMs" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForkStateLogs");

            migrationBuilder.DropTable(
                name: "PhilosopherStateLogs");

            migrationBuilder.DropTable(
                name: "SimulationRuns");
        }
    }
}
