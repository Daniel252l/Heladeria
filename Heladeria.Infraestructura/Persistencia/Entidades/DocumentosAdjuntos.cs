using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class DocumentosAdjuntos
{
    public long Id { get; set; }

    public int? EmpresaId { get; set; }

    public string? ModuloId { get; set; }

    public string Entidad { get; set; } = null!;

    public long EntidadId { get; set; }

    public string NombreArchivo { get; set; } = null!;

    public string RutaArchivo { get; set; } = null!;

    public string? TipoMime { get; set; }

    public long? TamanioBytes { get; set; }

    public DateTime FechaCarga { get; set; }

    public int UsuarioId { get; set; }

    public int EstadoId { get; set; }

    public virtual Empresas? Empresa { get; set; }

    public virtual Estados Estado { get; set; } = null!;

    public virtual Modulos? Modulo { get; set; }

    public virtual Usuarios Usuario { get; set; } = null!;
}
