using System.Net;
using System.Net.Http.Json;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Bootstrap;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Infraestructura.Seguridad.Autorizacion;
using Heladeria.Pruebas.Fase4;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Heladeria.Pruebas.Fase6;

public sealed class ProduccionIntegracionHttpPruebas
{
    [Fact]
    public async Task ReferenciasDeProduccionNoExigenPermisosDeOtrosModulos()
    {
        await using var fabrica = await CrearApiAsync();
        using var cliente = fabrica.CrearClienteAutenticado();
        var respuesta = await cliente.PostAsJsonAsync("/api/recetas", EscenarioFase6.SolicitudReceta());
        respuesta.EnsureSuccessStatusCode();
        var receta = (await respuesta.Content.ReadFromJsonAsync<RecetaConsultaDto>())!;
        using (var scope = fabrica.Services.CreateScope())
        {
            var contexto = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
            var concesiones = await contexto.RolesPermisos.Include(p => p.Permiso)
                .Where(p => p.RoleId == "ADMIN").ToArrayAsync();
            foreach (var permiso in concesiones)
                permiso.Permitido = permiso.Permiso.Codigo == PermisosSistema.ProduccionConsultar;
            await contexto.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Forbidden, (await cliente.GetAsync("/api/recetas/referencias")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/produccion/productos")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/produccion/recetas")).StatusCode);
        var consulta = await cliente.GetFromJsonAsync<RecetaConsultaDto>($"/api/produccion/recetas/{receta.Encabezado.Id}");
        Assert.Equal("ml", consulta!.AbreviaturaUnidadBase);
        Assert.False(consulta.FueUtilizada);
        Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/api/produccion/productos/1/presentaciones")).StatusCode);
        // Conserva la validación existente: un producto ajeno no es una selección válida.
        Assert.Equal(HttpStatusCode.BadRequest, (await cliente.GetAsync("/api/produccion/productos/3/presentaciones")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync("/api/produccion/recetas/999999")).StatusCode);
    }

    [Fact]
    public async Task FlujoHttpCreaRecetaConfirmaYFinalizaProduccion()
    {
        await using var fabrica = await CrearApiAsync();
        using var cliente = fabrica.CrearClienteAutenticado();
        var creada = await cliente.PostAsJsonAsync("/api/recetas", EscenarioFase6.SolicitudReceta());
        Assert.Equal(HttpStatusCode.Created, creada.StatusCode);
        var receta = (await creada.Content.ReadFromJsonAsync<RecetaConsultaDto>())!;
        await FijarVersionAsync(fabrica, receta.Encabezado.Id, receta: true, 31);
        receta = (await cliente.GetFromJsonAsync<RecetaConsultaDto>($"/api/recetas/{receta.Encabezado.Id}"))!;

        var ordenCreada = await cliente.PostAsJsonAsync("/api/produccion",
            EscenarioFase6.SolicitudOrden(receta.Encabezado.Id));
        Assert.Equal(HttpStatusCode.Created, ordenCreada.StatusCode);
        var orden = (await ordenCreada.Content.ReadFromJsonAsync<OrdenProduccionConsultaDto>())!;
        await FijarVersionAsync(fabrica, orden.Encabezado.Id, receta: false, 41);
        orden = (await cliente.GetFromJsonAsync<OrdenProduccionConsultaDto>($"/api/produccion/{orden.Encabezado.Id}"))!;
        var confirmacion = await cliente.PostAsJsonAsync($"/api/produccion/{orden.Encabezado.Id}/confirmar",
            new CambiarEstadoProduccionSolicitud { VersionFila = orden.Encabezado.VersionFila });
        Assert.Equal(HttpStatusCode.OK, confirmacion.StatusCode);
        await FijarVersionAsync(fabrica, orden.Encabezado.Id, receta: false, 42);
        orden = (await cliente.GetFromJsonAsync<OrdenProduccionConsultaDto>($"/api/produccion/{orden.Encabezado.Id}"))!;

        var finalizada = await cliente.PostAsJsonAsync($"/api/produccion/{orden.Encabezado.Id}/finalizar",
            new FinalizarProduccionSolicitud
            {
                ClaveOperacion = Guid.NewGuid(), UbicacionId = 1,
                CantidadProducida = 1000m, VersionFila = orden.Encabezado.VersionFila
            });

        Assert.Equal(HttpStatusCode.OK, finalizada.StatusCode);
        var resultado = (await finalizada.Content.ReadFromJsonAsync<OrdenProduccionConsultaDto>())!;
        Assert.Equal("FINALIZADA", resultado.Encabezado.Estado);
        Assert.Equal(2, resultado.MovimientoIds.Count);
        Assert.Equal(5m, resultado.ProduccionTerminada!.CostoMateriaPrima);
        Assert.Equal(HttpStatusCode.Conflict,
            (await cliente.PostAsJsonAsync($"/api/produccion/{orden.Encabezado.Id}/finalizar",
                new FinalizarProduccionSolicitud
                {
                    ClaveOperacion = Guid.NewGuid(), UbicacionId = 1,
                    CantidadProducida = 1000m, VersionFila = resultado.Encabezado.VersionFila
                })).StatusCode);
    }

    private static async Task<FabricaApiSeguridad> CrearApiAsync()
    {
        var fabrica = new FabricaApiSeguridad(true, servicios =>
        {
            servicios.RemoveAll<IServicioPermisos>();
            servicios.AddScoped<IServicioPermisos, ServicioPermisos>();
        });
        using var scope = fabrica.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
        var coordinador = scope.ServiceProvider.GetRequiredService<CoordinadorAjustesInventario>();
        await using var fase5 = new EscenarioFase5(
            await EscenarioFase4.CrearConContextoAsync(contexto, coordinador), coordinador);
        await fase5.SemillarAsync();
        contexto.Existencias.Add(new Existencias
        {
            EmpresaId = 1, ProductoId = 2, BodegaId = 1, UbicacionId = 1,
            Cantidad = 100m, CantidadReservada = 0m, CostoPromedio = 0.02m,
            VersionFila = [1]
        });
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        await scope.ServiceProvider.GetRequiredService<IPreparadorFase6>().EjecutarAsync(default);
        return fabrica;
    }

    private static async Task FijarVersionAsync(
        FabricaApiSeguridad fabrica, long id, bool receta, byte valor)
    {
        using var scope = fabrica.Services.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<HeladeriaDbContext>();
        if (receta)
            (await contexto.Recetas.SingleAsync(item => item.Id == id)).VersionFila = [valor];
        else
            (await contexto.OrdenesProduccion.SingleAsync(item => item.Id == id)).VersionFila = [valor];
        await contexto.SaveChangesAsync();
    }
}
