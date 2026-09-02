using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Promociones
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public int TipoPromocionId { get; set; }

    public decimal? DescuentoPorcentaje { get; set; }

    public decimal? DescuentoMonto { get; set; }

    public decimal? PrecioEspecial { get; set; }

    public decimal? CantidadMinima { get; set; }

    public decimal? MontoMinimo { get; set; }

    public int? LimiteUsos { get; set; }

    public int? LimitePorCliente { get; set; }

    public DateTime FechaInicio { get; set; }

    public DateTime FechaFin { get; set; }

    public bool AplicaTodasSucursales { get; set; }

    public int EstadoId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<PromocionCategorias> PromocionCategorias { get; set; } = new List<PromocionCategorias>();

    public virtual ICollection<PromocionHorarios> PromocionHorarios { get; set; } = new List<PromocionHorarios>();

    public virtual ICollection<PromocionProductos> PromocionProductos { get; set; } = new List<PromocionProductos>();

    public virtual ICollection<PromocionSucursales> PromocionSucursales { get; set; } = new List<PromocionSucursales>();

    public virtual Tipos TipoPromocion { get; set; } = null!;
}
