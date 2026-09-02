using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class UnidadesMedida
{
    public int Id { get; set; }

    public int EmpresaId { get; set; }

    public string Codigo { get; set; } = null!;

    public string Nombre { get; set; } = null!;

    public string Abreviatura { get; set; } = null!;

    public bool PermiteDecimales { get; set; }

    public byte CantidadDecimales { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? ModificadoPorUsuarioId { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual ICollection<ConsumosProduccion> ConsumosProduccion { get; set; } = new List<ConsumosProduccion>();

    public virtual ICollection<ConversionesUnidad> ConversionesUnidadUnidadDestino { get; set; } = new List<ConversionesUnidad>();

    public virtual ICollection<ConversionesUnidad> ConversionesUnidadUnidadOrigen { get; set; } = new List<ConversionesUnidad>();

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Usuarios? ModificadoPorUsuario { get; set; }

    public virtual ICollection<MovimientoDetalles> MovimientoDetalles { get; set; } = new List<MovimientoDetalles>();

    public virtual ICollection<OrdenProduccionDetalles> OrdenProduccionDetalles { get; set; } = new List<OrdenProduccionDetalles>();

    public virtual ICollection<ProductoPresentaciones> ProductoPresentaciones { get; set; } = new List<ProductoPresentaciones>();

    public virtual ICollection<Productos> Productos { get; set; } = new List<Productos>();

    public virtual ICollection<RecetaDetalles> RecetaDetalles { get; set; } = new List<RecetaDetalles>();

    public virtual ICollection<Recetas> Recetas { get; set; } = new List<Recetas>();

    public virtual ICollection<TrasladoInventarioDetalles> TrasladoInventarioDetalles { get; set; } = new List<TrasladoInventarioDetalles>();
}
