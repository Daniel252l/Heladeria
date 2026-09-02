using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class Tipos
{
    public int Id { get; set; }

    public string Nombre { get; set; } = null!;

    public int EstadoId { get; set; }

    public string? ModuloId { get; set; }

    public int ClaseTipoId { get; set; }

    public virtual ICollection<AsistenciaPersonal> AsistenciaPersonal { get; set; } = new List<AsistenciaPersonal>();

    public virtual ClaseTipos ClaseTipo { get; set; } = null!;

    public virtual ICollection<Configuraciones> ConfiguracionesTipoConfiguracion { get; set; } = new List<Configuraciones>();

    public virtual ICollection<Configuraciones> ConfiguracionesTipoValor { get; set; } = new List<Configuraciones>();

    public virtual ICollection<CostosProduccion> CostosProduccion { get; set; } = new List<CostosProduccion>();

    public virtual ICollection<CuentasBancarias> CuentasBancarias { get; set; } = new List<CuentasBancarias>();

    public virtual ICollection<DenominacionesMoneda> DenominacionesMoneda { get; set; } = new List<DenominacionesMoneda>();

    public virtual ICollection<DireccionesClientes> DireccionesClientes { get; set; } = new List<DireccionesClientes>();

    public virtual ICollection<Empleados> Empleados { get; set; } = new List<Empleados>();

    public virtual ICollection<Empresas> Empresas { get; set; } = new List<Empresas>();

    public virtual Estados Estado { get; set; } = null!;

    public virtual ICollection<ListasPrecios> ListasPrecios { get; set; } = new List<ListasPrecios>();

    public virtual Modulos? Modulo { get; set; }

    public virtual ICollection<MovimientosBancarios> MovimientosBancarios { get; set; } = new List<MovimientosBancarios>();

    public virtual ICollection<MovimientosCaja> MovimientosCaja { get; set; } = new List<MovimientosCaja>();

    public virtual ICollection<MovimientosPuntos> MovimientosPuntos { get; set; } = new List<MovimientosPuntos>();

    public virtual ICollection<Pagos> Pagos { get; set; } = new List<Pagos>();

    public virtual ICollection<Promociones> Promociones { get; set; } = new List<Promociones>();

    public virtual ICollection<ReglasAutorizacion> ReglasAutorizacion { get; set; } = new List<ReglasAutorizacion>();

    public virtual ICollection<Reportes> Reportes { get; set; } = new List<Reportes>();

    public virtual ICollection<Roles> Roles { get; set; } = new List<Roles>();

    public virtual ICollection<SeriesDocumentos> SeriesDocumentos { get; set; } = new List<SeriesDocumentos>();

    public virtual TipoNotificaciones? TipoNotificaciones { get; set; }

    public virtual ICollection<Usuarios> Usuarios { get; set; } = new List<Usuarios>();
}
