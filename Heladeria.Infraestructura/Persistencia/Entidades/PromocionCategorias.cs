using System;
using System.Collections.Generic;

namespace Heladeria.Infraestructura.Persistencia.Entidades;

public partial class PromocionCategorias
{
    public long Id { get; set; }

    public long PromocionId { get; set; }

    public int CategoriaId { get; set; }

    public virtual Categorias Categoria { get; set; } = null!;

    public virtual Promociones Promocion { get; set; } = null!;
}
