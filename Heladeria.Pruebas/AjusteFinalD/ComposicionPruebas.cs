using Heladeria.Aplicacion.Productos;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.AjusteFinalD;

public sealed class ComposicionPruebas
{
    [Fact]
    public async Task CrearEditarEliminarYClienteAnteriorConservaComposicion()
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var p = await e.Productos.ObtenerAsync(e.Padre, EscenarioComposicion.S, default);
        Assert.True(p.EsCombo); Assert.Equal(5, p.Componentes.Count);
        Assert.Equal(120, p.Componentes[0].CantidadBase); Assert.Equal("un", p.Componentes[0].AbreviaturaUnidad);
        Assert.Equal(5, (await e.EditarAsync(null)).Componentes.Count);
        var cambiada = await e.EditarAsync([new() { ProductoId = 1, ProductoPresentacionId = e.Bola, Cantidad = 3 }]);
        Assert.Equal(180, Assert.Single(cambiada.Componentes).CantidadBase);
        var vacia = await e.EditarAsync([]); Assert.Empty(vacia.Componentes); Assert.False(vacia.EsCombo);
        Assert.Empty(await e.Db.CombosDetalle.ToArrayAsync());
        Assert.True(await e.Db.Bitacoras.AnyAsync(x => x.Entidad == "Productos" && x.Accion == "ACTUALIZAR"));
    }

    [Theory]
    [InlineData("cero")][InlineData("negativo")][InlineData("precision")][InlineData("inactivo")]
    [InlineData("mismo")][InlineData("duplicado")][InlineData("ajeno")][InlineData("presentacion-ajena")]
    [InlineData("presentacion-inactiva")][InlineData("no-inventariable")][InlineData("lote")][InlineData("ciclo")]
    public async Task ConfiguracionInvalidaNoModificaComposicion(string caso)
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        long producto = 1; long? pp = e.Bola; decimal cantidad = 2;
        switch (caso)
        {
            case "cero": cantidad = 0; break;
            case "negativo": cantidad = -1; break;
            case "precision": cantidad = .0000001m; break;
            case "inactivo": (await e.Db.Productos.SingleAsync(x => x.Id == 1)).EstadoId = 2; break;
            case "mismo": producto = e.Padre; pp = null; break;
            case "ajeno": producto = 3; pp = null; break;
            case "presentacion-ajena": producto = e.Insumos[0]; break;
            case "presentacion-inactiva": (await e.Db.ProductoPresentaciones.SingleAsync(x => x.Id == e.Bola)).EstadoId = 2; break;
            case "no-inventariable": (await e.Db.TiposProductos.SingleAsync(x => x.Id == 1)).ManejaExistencia = false; break;
            case "lote": (await e.Db.Productos.SingleAsync(x => x.Id == 1)).ControlaVencimiento = true; break;
            case "ciclo": producto = e.Padre; pp = null; break;
        }
        await e.Db.SaveChangesAsync(); e.Db.ChangeTracker.Clear();
        var fila = new ComponenteVentaSolicitud { ProductoId = producto, ProductoPresentacionId = pp, Cantidad = cantidad };
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => e.EditarAsync(caso == "duplicado" ? [fila, fila] : [fila], caso == "ciclo" ? 1 : null));
        Assert.Equal(5, await e.Db.CombosDetalle.CountAsync());
        Assert.Empty(await e.Db.Movimientos.ToArrayAsync());
    }

    [Fact]
    public async Task AnidamientoIndirectoVersionYAlcance()
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        await Assert.ThrowsAsync<ValidacionFase4Exception>(() => e.Productos.CrearAsync(e.Solicitud("OTRO-COMPUESTO",
            [new() { ProductoId = e.Padre, Cantidad = 1 }]), EscenarioComposicion.S, default));
        await Assert.ThrowsAsync<ConflictoFase4Exception>(() => e.EditarAsync([], version: "AQID"));
        await Assert.ThrowsAsync<RecursoFase4NoEncontradoException>(() => e.Productos.ObtenerAsync(e.Padre,
            EscenarioComposicion.S with { EmpresaId = 2 }, default));
        var opciones = await e.Productos.ListarAsync(new() { SoloComponentesVenta = true }, EscenarioComposicion.S, default);
        Assert.DoesNotContain(opciones.Elementos, x => x.Id == e.Padre || x.EmpresaId != 1);
        Assert.All(e.Insumos, id => Assert.Contains(opciones.Elementos, x => x.Id == id && !x.EsVendible));
    }

    [Fact]
    public async Task FalloPersistiendoComposicionRevierteProductoYAuditoria()
    {
        await using var e = await EscenarioComposicion.CrearAsync();
        var productos = await e.Db.Productos.CountAsync(); var auditorias = await e.Db.Bitacoras.CountAsync();
        await e.Db.Database.ExecuteSqlRawAsync("CREATE TRIGGER FalloComposicion BEFORE INSERT ON CombosDetalle BEGIN SELECT RAISE(ABORT,'Fallo deliberado'); END;");
        await Assert.ThrowsAsync<DbUpdateException>(() => e.Productos.CrearAsync(e.Solicitud("FALLIDA", e.Componentes()), EscenarioComposicion.S, default));
        Assert.Equal(productos, await e.Db.Productos.CountAsync()); Assert.Equal(auditorias, await e.Db.Bitacoras.CountAsync());
        Assert.Equal(5, await e.Db.CombosDetalle.CountAsync());
    }
}
