using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CorrelativosDocumentos
{
    public long Id { get; set; }

    public long SerieDocumentoId { get; set; }

    public long UltimoNumero { get; set; }

    public DateTime FechaModificacion { get; set; }

    public byte[] VersionFila { get; set; } = null!;

    public virtual SeriesDocumentos SerieDocumento { get; set; } = null!;
}
