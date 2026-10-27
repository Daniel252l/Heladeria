using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CombosDetalle
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ComboProductoId { get; set; }

    public long ProductoIncluidoId { get; set; }

    public decimal Cantidad { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public virtual ProductoPresentaciones? ProductoPresentacion { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Productos Productos { get; set; } = null!;

    public virtual Productos ProductosNavigation { get; set; } = null!;
}
