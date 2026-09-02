using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class FormasPago
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public bool RequiereReferencia { get; set; }

    public bool AfectaCaja { get; set; }

    public bool PermiteCambio { get; set; }

    public bool EsCredito { get; set; }

    public int EstadoId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<MovimientosCaja> MovimientosCaja { get; set; } = new List<MovimientosCaja>();

    public virtual ICollection<PagoDetalles> PagoDetalles { get; set; } = new List<PagoDetalles>();
}
