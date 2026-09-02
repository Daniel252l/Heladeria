using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class AsistenciaPersonal
{
    public long Id { get; set; }

    public int EmpleadoId { get; set; }

    public long SucursalId { get; set; }

    public DateTime FechaHoraEntrada { get; set; }

    public DateTime? FechaHoraSalida { get; set; }

    public int TipoTurnoId { get; set; }

    public bool Aprobado { get; set; }

    public virtual Empleados Empleado { get; set; } = null!;

    public virtual Sucursales Sucursal { get; set; } = null!;

    public virtual Tipos TipoTurno { get; set; } = null!;
}
