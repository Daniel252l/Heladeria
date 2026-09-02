using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class HistoricoCostosProductos
{
    public long Id { get; set; }

    public int EmpresaId { get; set; }

    public long ProductoId { get; set; }

    public decimal? CostoAnterior { get; set; }

    public decimal CostoNuevo { get; set; }

    public DateTime FechaCambio { get; set; }

    public string OrigenCambio { get; set; } = null!;

    public long? DocumentoOrigenId { get; set; }

    public int? UsuarioId { get; set; }

    public virtual Empresas Empresa { get; set; } = null!;

    public virtual Productos Producto { get; set; } = null!;

    public virtual Usuarios? Usuario { get; set; }
}
