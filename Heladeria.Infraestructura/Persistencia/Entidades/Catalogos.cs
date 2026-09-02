using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Catalogos
{
    public long Id { get; set; }

    public int ClaseCatalogoId { get; set; }

    public int? EmpresaId { get; set; }

    public long? CatalogoPadreId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string? Descripcion { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public virtual Catalogos? CatalogosNavigation { get; set; }

    public virtual ClasesCatalogos ClaseCatalogo { get; set; } = null!;

    public virtual ICollection<CodigosBarrasProductos> CodigosBarrasProductos { get; set; } = new List<CodigosBarrasProductos>();

    public virtual ICollection<ConteosFisicos> ConteosFisicos { get; set; } = new List<ConteosFisicos>();

    public virtual ICollection<CostosProductos> CostosProductos { get; set; } = new List<CostosProductos>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual ICollection<DevolucionesVentas> DevolucionesVentas { get; set; } = new List<DevolucionesVentas>();

    public virtual Empresas? Empresa { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<Catalogos> InverseCatalogosNavigation { get; set; } = new List<Catalogos>();

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<Movimientos> Movimientos { get; set; } = new List<Movimientos>();

    public virtual ICollection<Productos> ProductosMetodoValuacion { get; set; } = new List<Productos>();

    public virtual ICollection<Productos> ProductosTipoSeguimiento { get; set; } = new List<Productos>();

    public virtual ICollection<Ubicaciones> Ubicaciones { get; set; } = new List<Ubicaciones>();
}
