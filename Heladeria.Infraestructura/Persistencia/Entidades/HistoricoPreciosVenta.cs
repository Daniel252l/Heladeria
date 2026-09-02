using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class HistoricoPreciosVenta
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public decimal? PrecioAnterior { get; set; }

    public decimal PrecioNuevo { get; set; }

    public DateTime FechaCambio { get; set; }

    public int UsuarioId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}
