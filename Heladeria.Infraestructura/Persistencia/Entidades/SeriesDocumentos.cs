using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class SeriesDocumentos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long? SucursalId { get; set; }

    public int TipoDocumentoId { get; set; }

    public string Serie { get; set; } = null!;

    public string? Prefijo { get; set; }

    public byte Longitud { get; set; }

    public int EstadoId { get; set; }

    public virtual CorrelativosDocumentos? CorrelativosDocumentos { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual Sucursales? Sucursal { get; set; }

    public virtual Tipos TipoDocumento { get; set; } = null!;
}
