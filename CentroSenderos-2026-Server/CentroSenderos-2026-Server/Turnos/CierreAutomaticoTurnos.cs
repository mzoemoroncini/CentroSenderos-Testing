using CentroSenderos_2026_BD;
using CentroSenderos_2026_Shared.Enum;
using Microsoft.EntityFrameworkCore;

namespace CentroSenderos_2026_Server.Turnos;

internal sealed class CierreAutomaticoTurnos(
    IServiceScopeFactory scopes,
    IConfiguration configuracion,
    ILogger<CierreAutomaticoTurnos> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(configuracion["CierreAutomaticoTurnos:Desde"], "yyyy-MM-dd", out var fechaInicio))
        {
            logger.LogInformation("El cierre automático de turnos requiere configurar CierreAutomaticoTurnos:Desde.");
            return;
        }

        var desde = fechaInicio.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
        do
        {
            try
            {
                await CerrarTurnos(desde, cancellationToken);
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogError(ex, "No se pudo ejecutar el cierre automático de turnos.");
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    private async Task CerrarTurnos(DateTime desde, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ahoraUtc = DateTime.UtcNow;
        // La agenda guarda la hora local de Senderos etiquetada como UTC.
        var horaAgenda = DateTime.SpecifyKind(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(ahoraUtc, "America/Argentina/Cordoba"),
            DateTimeKind.Utc);
        var limite = horaAgenda.AddHours(-24);

        var pendientes = db.Turnos.Where(turno =>
            turno.EstadoRegistro == EnumEstadoRegistro.activo &&
            turno.EstadoTurno == EnumEstadoTurno.reservado &&
            !turno.TieneIntervencionManual &&
            turno.FechaInicio >= desde &&
            turno.FechaFin <= limite &&
            !turno.TurnoPacientes.Any(p => p.EstadoRegistro == EnumEstadoRegistro.activo && p.Asistencia != null));

        var ultimoId = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var turnos = await pendientes.Where(t => t.Id > ultimoId)
                .OrderBy(t => t.Id)
                .Take(100)
                .Include(t => t.TurnoPacientes.Where(p => p.EstadoRegistro == EnumEstadoRegistro.activo))
                .ToListAsync(cancellationToken);

            if (turnos.Count == 0)
                return;

            foreach (var turno in turnos)
            {
                turno.RevisionCierre++;
                turno.EstadoTurno = EnumEstadoTurno.atendido;
                turno.CierreAutomaticoUtc = ahoraUtc;
                var pacientes = turno.TurnoPacientes.ToArray();
                foreach (var paciente in pacientes)
                {
                    paciente.Asistencia = EnumAsistenciaPaciente.asistio;
                    paciente.AsistenciaAutomatica = true;
                }

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException)
                {
                    db.Entry(turno).State = EntityState.Detached;
                    foreach (var paciente in pacientes)
                        db.Entry(paciente).State = EntityState.Detached;
                }
            }

            ultimoId = turnos[^1].Id;
            db.ChangeTracker.Clear();
        }
    }
}
