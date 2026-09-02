using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class CodigosBarrasProductos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public long? ProductoPresentacionId { get; set; }

    public string CodigoBarras { get; set; } = null!;

    public long TipoCodigoBarrasId { get; set; }

    public bool EsPrincipal { get; set; }

    public int EstadoId { get; set; }

    public DateTime FechaCreacion { get; set; }

    public int CreadoPorUsuarioId { get; set; }

    public virtual Usuarios CreadoPorUsuario { get; set; } = null!;

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Estados Estado { get; set; } = null!;

    public virtual ProductoPresentaciones? ProductoPresentaciones { get; set; }

    public virtual Productos Productos { get; set; } = null!;

    public virtual Catalogos TipoCodigoBarras { get; set; } = null!;
}
