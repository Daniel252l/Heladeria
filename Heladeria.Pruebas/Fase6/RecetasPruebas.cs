using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase6;

public sealed class RecetasPruebas
{
    [Fact]
    public async Task ConsultaExponeUsoYUnidadesSinModificarLaReceta()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        Assert.False(receta.FueUtilizada);
        Assert.Equal("ml", receta.AbreviaturaUnidadBase);
        Assert.Null(receta.PresentacionRendimiento); // rendimiento en unidad base

        await e.CrearOrdenAsync(receta.Encabezado.Id);
        var usada = await e.Recetas.ObtenerAsync(receta.Encabezado.Id, EscenarioFase6.Seguridad, default);
        Assert.True(usada.FueUtilizada);
        Assert.Equal(receta.Encabezado, usada.Encabezado);
        Assert.Equal(receta.Detalles, usada.Detalles);
        Assert.False(e.Contexto.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task ReferenciasYBusquedaDeProductosRespetanEmpresaYPaginacion()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var referencias = await e.Recetas.ObtenerReferenciasAsync(EscenarioFase6.Seguridad, default);
        var esperados = await e.Contexto.Productos.AsNoTracking()
            .Where(p => p.EmpresaId == EscenarioFase6.Seguridad.EmpresaId)
            .OrderBy(p => p.Nombre).Select(p => p.Id).ToArrayAsync();
        Assert.Equal(esperados, referencias.Productos.Select(p => p.Id));
        Assert.DoesNotContain(referencias.Productos, p => p.Id == 3);
        Assert.NotEmpty(referencias.Estados);

        var pagina = await e.Recetas.ListarProductosAsync(new()
        { Busqueda = referencias.Productos[0].Codigo, Pagina = 1, TamanioPagina = 1 },
            EscenarioFase6.Seguridad, default);
        Assert.Equal(referencias.Productos[0], Assert.Single(pagina.Elementos));
        Assert.False(e.Contexto.ChangeTracker.HasChanges());
    }

    [Fact]
    public async Task FiltroVersionSeAplicaAntesDePaginar()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        await e.CrearOrdenAsync(receta.Encabezado.Id);
        var siguiente = await e.Recetas.ActualizarAsync(receta.Encabezado.Id,
            Actualizacion(receta, "Versión 2", receta.Encabezado.VigenteDesde.AddDays(1)),
            EscenarioFase6.Seguridad, default);
        var pagina = await e.Recetas.ListarAsync(new() { Codigo = "rec-vai", Version = 2, TamanioPagina = 1 },
            EscenarioFase6.Seguridad, default);
        Assert.Equal(1, pagina.TotalRegistros);
        Assert.Equal(siguiente.Encabezado.Id, Assert.Single(pagina.Elementos).Id);
        Assert.Empty((await e.Recetas.ListarAsync(new() { Version = 3 },
            EscenarioFase6.Seguridad, default)).Elementos);
        Assert.Empty((await e.Recetas.ListarAsync(new() { Codigo = "REC-VA" },
            EscenarioFase6.Seguridad, default)).Elementos);
    }

    [Fact]
    public async Task PreparacionEsIdempotenteYNoReiniciaCorrelativos()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        await e.CrearOrdenAsync(receta.Encabezado.Id);
        var permisos = await e.Contexto.Permisos.CountAsync();
        var estados = await e.Contexto.EstadosFlujo.CountAsync();

        var segunda = await e.Preparador.EjecutarAsync(default);

        Assert.Empty(segunda.Registros);
        Assert.Equal(permisos, await e.Contexto.Permisos.CountAsync());
        Assert.Equal(estados, await e.Contexto.EstadosFlujo.CountAsync());
        Assert.Equal(1, (await e.Contexto.CorrelativosDocumentos
            .SingleAsync(item => item.SerieDocumento.Serie == "OP")).UltimoNumero);
        Assert.Equal(1, await e.Contexto.Bitacoras.CountAsync(item => item.Accion == "FASE6_CONFIGURADA"));
        Assert.Equal(5, await e.Contexto.Permisos.CountAsync(item =>
            item.ModuloId == "PRODUCCION" || item.ModuloId == "RECETAS"));
    }

    [Fact]
    public async Task CrearRecetaPersisteSnapshotMultimedidaYCantidadBase()
    {
        await using var e = await EscenarioFase6.CrearAsync();

        var receta = await e.CrearRecetaAsync();

        Assert.Equal(1000m, receta.Encabezado.RendimientoBase);
        Assert.Equal(1m, receta.Encabezado.FactorConversion);
        var ingrediente = Assert.Single(receta.Detalles);
        Assert.Equal(1, ingrediente.ProductoPresentacionId);
        Assert.Equal(0.5m, ingrediente.Cantidad);
        Assert.Equal(1000m, ingrediente.FactorConversion);
        Assert.Equal(500m, ingrediente.CantidadBase);
        Assert.Equal("ml", ingrediente.AbreviaturaUnidadBase);
        Assert.Equal(0m, await e.Contexto.RecetaDetalles
            .Where(item => item.Id == ingrediente.Id)
            .Select(item => item.PorcentajeDesperdicioTecnico).SingleAsync());
    }

    [Fact]
    public async Task IngredientePropioYProductoAjenoSonRechazados()
    {
        await using var e = await EscenarioFase6.CrearAsync();

        await Assert.ThrowsAsync<ValidacionProduccionException>(() => e.Recetas.CrearAsync(
            EscenarioFase6.SolicitudReceta(terminado: 2, ingrediente: 2,
                presentacionIngrediente: null, codigo: "AUTO"),
            EscenarioFase6.Seguridad, default));
        await Assert.ThrowsAsync<ValidacionProduccionException>(() => e.Recetas.CrearAsync(
            EscenarioFase6.SolicitudReceta(terminado: 3, ingrediente: 1,
                codigo: "AJENA"),
            EscenarioFase6.Seguridad, default));
        Assert.Empty(e.Contexto.Recetas);
    }

    [Fact]
    public async Task CicloIndirectoEsRechazadoSinAlterarRecetaExistente()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var original = await e.CrearRecetaAsync(); // Vainilla -> Chocolate

        await Assert.ThrowsAsync<ValidacionProduccionException>(() => e.Recetas.CrearAsync(
            EscenarioFase6.SolicitudReceta(terminado: 1, ingrediente: 2,
                presentacionIngrediente: null, codigo: "REC-CHO"),
            EscenarioFase6.Seguridad, default));

        Assert.Single(e.Contexto.Recetas);
        Assert.Null((await e.Contexto.Recetas.SingleAsync(item => item.Id == original.Encabezado.Id)).VigenteHasta);
    }

    [Fact]
    public async Task RecetaNoUsadaSeActualizaYRecetaUsadaGeneraNuevaVersion()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var actualizada = await e.Recetas.ActualizarAsync(receta.Encabezado.Id,
            Actualizacion(receta, "Receta corregida", receta.Encabezado.VigenteDesde),
            EscenarioFase6.Seguridad, default);
        Assert.Equal(receta.Encabezado.Id, actualizada.Encabezado.Id);
        Assert.Equal(1, actualizada.Encabezado.Version);
        Assert.Equal("Receta corregida", actualizada.Encabezado.Nombre);

        await e.FijarVersionRecetaAsync(actualizada.Encabezado.Id, 12);
        actualizada = await e.Recetas.ObtenerAsync(actualizada.Encabezado.Id, EscenarioFase6.Seguridad, default);
        await e.CrearOrdenAsync(actualizada.Encabezado.Id);
        var nueva = await e.Recetas.ActualizarAsync(actualizada.Encabezado.Id,
            Actualizacion(actualizada, "Receta v2", actualizada.Encabezado.VigenteDesde.AddDays(1)),
            EscenarioFase6.Seguridad, default);

        Assert.NotEqual(actualizada.Encabezado.Id, nueva.Encabezado.Id);
        Assert.Equal(2, nueva.Encabezado.Version);
        Assert.Null(nueva.Encabezado.VigenteHasta);
        var historica = await e.Contexto.Recetas.AsNoTracking()
            .SingleAsync(item => item.Id == actualizada.Encabezado.Id);
        Assert.Equal(actualizada.Encabezado.VigenteDesde, historica.VigenteHasta);
        Assert.Equal(2, await e.Contexto.Recetas.CountAsync());
        Assert.Single(await e.Contexto.Recetas.Where(item => item.VigenteHasta == null).ToArrayAsync());
    }

    [Fact]
    public async Task VersionDesactualizadaYAlcanceDeSucursalSeControlan()
    {
        await using var e = await EscenarioFase6.CrearAsync();
        var receta = await e.CrearRecetaAsync();
        var solicitud = Actualizacion(receta, "Cambio", receta.Encabezado.VigenteDesde);
        solicitud = new ActualizarRecetaSolicitud
        {
            ProductoTerminadoId = solicitud.ProductoTerminadoId,
            Codigo = solicitud.Codigo,
            Nombre = solicitud.Nombre,
            Rendimiento = solicitud.Rendimiento,
            VigenteDesde = solicitud.VigenteDesde,
            EstadoId = solicitud.EstadoId,
            Detalles = solicitud.Detalles,
            VersionFila = Convert.ToBase64String([99])
        };
        await Assert.ThrowsAsync<ConflictoProduccionException>(() => e.Recetas.ActualizarAsync(
            receta.Encabezado.Id, solicitud, EscenarioFase6.Seguridad, default));

        var otraSucursal = EscenarioFase6.Seguridad with { SucursalId = 2 };
        await Assert.ThrowsAsync<RecursoProduccionNoEncontradoException>(() =>
            e.Recetas.ObtenerAsync(999, otraSucursal, default));
        Assert.Equal("Receta de prueba", (await e.Recetas.ObtenerAsync(
            receta.Encabezado.Id, EscenarioFase6.Seguridad, default)).Encabezado.Nombre);
    }

    private static ActualizarRecetaSolicitud Actualizacion(
        RecetaConsultaDto receta, string nombre, DateOnly vigenteDesde) => new()
    {
        ProductoTerminadoId = receta.Encabezado.ProductoTerminadoId,
        Codigo = receta.Encabezado.Codigo,
        Nombre = nombre,
        Rendimiento = receta.Encabezado.Rendimiento,
        ProductoPresentacionRendimientoId = receta.Encabezado.ProductoPresentacionRendimientoId,
        VigenteDesde = vigenteDesde,
        EstadoId = receta.Encabezado.EstadoId,
        VersionFila = receta.Encabezado.VersionFila,
        Detalles = receta.Detalles.Select(item => new RecetaDetalleSolicitud
        {
            ProductoInsumoId = item.ProductoInsumoId,
            ProductoPresentacionId = item.ProductoPresentacionId,
            Cantidad = item.Cantidad,
            Observacion = item.Observacion
        }).ToArray()
    };
}
