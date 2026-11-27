using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CentroSenderos_2026_BD.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261127200000_CierreAutomaticoTurnos")]
public sealed class CierreAutomaticoTurnos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>("CierreAutomaticoUtc", "Turnos", "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<long>("RevisionCierre", "Turnos", "bigint", nullable: false, defaultValue: 0L);
        migrationBuilder.AddColumn<bool>("TieneIntervencionManual", "Turnos", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<bool>("AsistenciaAutomatica", "TurnoPaciente", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<string>("UltimaAsistenciaRegistradaPorId", "TurnoPaciente", "text", nullable: true);
        migrationBuilder.AddColumn<DateTime>("UltimaAsistenciaRegistradaUtc", "TurnoPaciente", "timestamp with time zone", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("UltimaAsistenciaRegistradaUtc", "TurnoPaciente");
        migrationBuilder.DropColumn("UltimaAsistenciaRegistradaPorId", "TurnoPaciente");
        migrationBuilder.DropColumn("AsistenciaAutomatica", "TurnoPaciente");
        migrationBuilder.DropColumn("TieneIntervencionManual", "Turnos");
        migrationBuilder.DropColumn("RevisionCierre", "Turnos");
        migrationBuilder.DropColumn("CierreAutomaticoUtc", "Turnos");
    }
}
