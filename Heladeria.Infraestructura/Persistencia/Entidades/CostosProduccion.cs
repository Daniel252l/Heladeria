using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CostosProduccion
{
    public long Id { get; set; }

    public long OrdenProduccionId { get; set; }

    public int TipoCostoId { get; set; }

    public string Descripcion { get; set; } = null!;

    public decimal Monto { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int UsuarioId { get; set; }

    public virtual OrdenesProduccion OrdenProduccion { get; set; } = null!;

    public virtual Tipos TipoCosto { get; set; } = null!;

    public virtual Usuarios Usuario { get; set; } = null!;
}
