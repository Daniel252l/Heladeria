using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CostosProductos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public decimal CostoActual { get; set; }

    public DateTime FechaCalculo { get; set; }

    public long? MetodoValuacionId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Catalogos? MetodoValuacion { get; set; }

    public virtual Productos Producto { get; set; } = null!;
}
