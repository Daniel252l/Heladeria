using System.Text.Json;
using Heladeria.Api.ManejoErrores;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Configuracion;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Heladeria.Pruebas.Configuracion;

public sealed class ServiciosConfiguracionPruebas
{
    private static readonly ContextoConfiguracion Admin = new(1, "ADMIN", 1, 1, "127.0.0.1", "xUnit");
    private static readonly ContextoConfiguracion Consulta = new(2, "CONSULTA", 1, 1, "127.0.0.1", "xUnit");

    [Fact]
    public async Task ReferenciasOrganizacion_FiltraEmpresaBusquedaEstadoYPaginacionSinMutarDatos()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        escenario.Contexto.Proveedores.AddRange(
            new Proveedores { Id = 10, EmpresaId = 1, CodigoProveedor = "OTRO", NombreProveedor = "Proveedor adicional", DiasCredito = 0, EstadoId = 1 },
            new Proveedores { Id = 11, EmpresaId = 1, CodigoProveedor = "INACTIVO", NombreProveedor = "Proveedor inactivo", DiasCredito = 0, EstadoId = 2 });
        await escenario.Contexto.SaveChangesAsync();
        escenario.Contexto.ChangeTracker.Clear();

        var respuesta = await escenario.Organizacion.ListarReferenciasAsync(
            new FiltroReferenciasOrganizacionSolicitud { Tipo = "PROVEEDORES", EmpresaId = 1, Busqueda = "Proveedor", TamanioPagina = 1 },
            Admin,
            CancellationToken.None);

        Assert.Equal(2, respuesta.TotalRegistros);
        Assert.Single(respuesta.Elementos);
        Assert.DoesNotContain(respuesta.Elementos, item => item.Id == 2 || item.Id == 11);
        Assert.Empty(escenario.Contexto.ChangeTracker.Entries());
    }

    [Fact]
    public async Task ReferenciasOrganizacion_RespetaDepartamentoYAlcanceDeEmpresa()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync(tienePermiso: false);
        escenario.Contexto.AddRange(
            new Departamentos { Id = 10, Nombre = "Guatemala", Codigo = "01" },
            new Departamentos { Id = 20, Nombre = "Otro", Codigo = "02" },
            new Municipios { Id = 10, DepartamentoId = 10, Nombre = "Guatemala", Codigo = "0101" },
            new Municipios { Id = 20, DepartamentoId = 20, Nombre = "Otro municipio", Codigo = "0201" });
        await escenario.Contexto.SaveChangesAsync();

        var respuesta = await escenario.Organizacion.ListarReferenciasAsync(
            new FiltroReferenciasOrganizacionSolicitud { Tipo = "MUNICIPIOS", DepartamentoId = 10 },
            Consulta,
            CancellationToken.None);
        Assert.Equal(10, Assert.Single(respuesta.Elementos).Id);
        await Assert.ThrowsAsync<RecursoConfiguracionNoEncontradoException>(() => escenario.Organizacion.ListarReferenciasAsync(
            new FiltroReferenciasOrganizacionSolicitud { Tipo = "PROVEEDORES", EmpresaId = 2 }, Consulta, CancellationToken.None));
    }

    [Theory]
    [InlineData("PROVEEDORES")]
    [InlineData("CLIENTES")]
    [InlineData("NO_EXISTE")]
    public async Task ReferenciasOrganizacion_RechazaDependenciasInvalidas(string tipo)
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        await Assert.ThrowsAsync<ValidacionConfiguracionException>(() => escenario.Organizacion.ListarReferenciasAsync(
            new FiltroReferenciasOrganizacionSolicitud { Tipo = tipo }, Admin, CancellationToken.None));
    }

    [Fact]
    public async Task ConflictoConfiguracion_SeNormalizaComoHttp409()
    {
        var contextoHttp = new DefaultHttpContext();
        contextoHttp.Response.Body = new MemoryStream();
        var manejador = new ManejadorGlobalExcepciones(
            NullLogger<ManejadorGlobalExcepciones>.Instance);

        await manejador.TryHandleAsync(
            contextoHttp,
            new ConflictoConfiguracionException(
                "La empresa principal utilizada por la administración del sistema no puede inactivarse."),
            CancellationToken.None);

        contextoHttp.Response.Body.Position = 0;
        var respuesta = await JsonSerializer.DeserializeAsync<RespuestaError>(
            contextoHttp.Response.Body,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal(StatusCodes.Status409Conflict, contextoHttp.Response.StatusCode);
        Assert.Equal("CONFLICTO_CONFIGURACION", respuesta!.Codigo);
        Assert.Contains("no puede inactivarse", respuesta.Mensaje);
    }

    [Fact]
    public async Task Listados_AplicanFiltroPaginacionYOrdenEstable()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        escenario.Contexto.Modulos.AddRange(
            new Modulos { Id = "VENTAS", Nombre = "Ventas", Icono = "cart", Activo = true },
            new Modulos { Id = "BODEGA", Nombre = "Bodega", Icono = "box", Activo = false });
        await escenario.Contexto.SaveChangesAsync();

        var pagina = await escenario.Catalogos.ListarModulosAsync(
            new FiltroActivosSolicitud { Activo = true, Pagina = 1, TamanioPagina = 2 },
            CancellationToken.None);

        Assert.Equal(3, pagina.TotalRegistros);
        Assert.Equal(2, pagina.Elementos.Count);
        Assert.Equal(2, pagina.TotalPaginas);
        Assert.Equal(pagina.Elementos.OrderBy(item => item.Nombre).Select(item => item.Id), pagina.Elementos.Select(item => item.Id));

        var filtrado = await escenario.Catalogos.ListarModulosAsync(
            new FiltroActivosSolicitud { Busqueda = "Venta" },
            CancellationToken.None);
        Assert.Single(filtrado.Elementos);
        Assert.Equal("VENTAS", filtrado.Elementos[0].Id);
    }

    [Fact]
    public async Task ClaseEstado_PermiteCrearConsultarActualizarYCambiarEstado()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var creado = await escenario.EstadosTipos.CrearClaseEstadoAsync(
            new GuardarClaseEstadoSolicitud { Nombre = "  Operaciones  ", Activo = true }, Admin, CancellationToken.None);

        Assert.True(creado.Id > 1);
        Assert.Equal("Operaciones", (await escenario.EstadosTipos.ObtenerClaseEstadoAsync(creado.Id, CancellationToken.None)).Nombre);
        var actualizado = await escenario.EstadosTipos.ActualizarClaseEstadoAsync(
            creado.Id, new GuardarClaseEstadoSolicitud { Nombre = "Operación", Activo = true }, Admin, CancellationToken.None);
        Assert.Equal("Operación", actualizado.Nombre);
        Assert.False((await escenario.EstadosTipos.CambiarActivoClaseEstadoAsync(creado.Id, false, Admin, CancellationToken.None)).Activo);
        Assert.Equal(3, escenario.Auditoria.Cantidad);
    }

    [Fact]
    public async Task ClaseEstado_DuplicadoYNoEncontradoProducenErroresControlados()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() => escenario.EstadosTipos.CrearClaseEstadoAsync(
            new GuardarClaseEstadoSolicitud { Nombre = "Seguridad" }, Admin, CancellationToken.None));
        await Assert.ThrowsAsync<RecursoConfiguracionNoEncontradoException>(() =>
            escenario.EstadosTipos.ObtenerClaseEstadoAsync(999, CancellationToken.None));
    }

    [Fact]
    public async Task Estado_ValidaForaneasYProtegeActivoEstructural()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        await Assert.ThrowsAsync<ValidacionConfiguracionException>(() => escenario.EstadosTipos.CrearEstadoAsync(
            new GuardarEstadoSolicitud { Nombre = "Pendiente", ClaseEstadoId = 999 }, Admin, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.EstadosTipos.CambiarActivoEstadoAsync(1, false, Admin, CancellationToken.None));

        var creado = await escenario.EstadosTipos.CrearEstadoAsync(
            new GuardarEstadoSolicitud { Nombre = "Pendiente", ClaseEstadoId = 1, ModuloId = " configuracion " }, Admin, CancellationToken.None);
        Assert.Equal("CONFIGURACION", creado.ModuloId);
        var actualizado = await escenario.EstadosTipos.ActualizarEstadoAsync(
            creado.Id,
            new GuardarEstadoSolicitud { Nombre = "En proceso", ClaseEstadoId = 1, ModuloId = "CONFIGURACION", Activo = true },
            Admin,
            CancellationToken.None);
        Assert.Equal("En proceso", actualizado.Nombre);
        Assert.False((await escenario.EstadosTipos.CambiarActivoEstadoAsync(
            creado.Id, false, Admin, CancellationToken.None)).Activo);
    }

    [Fact]
    public async Task Tipos_ValidanDuplicadoForaneasYProtegenUsoEnSeguridad()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() => escenario.EstadosTipos.CrearClaseTipoAsync(
            new GuardarClaseTipoSolicitud { Nombre = "Seguridad", EstadoId = 1 }, Admin, CancellationToken.None));
        await Assert.ThrowsAsync<ValidacionConfiguracionException>(() => escenario.EstadosTipos.CrearTipoAsync(
            new GuardarTipoSolicitud { Nombre = "Otro", EstadoId = 1, ClaseTipoId = 999 }, Admin, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.EstadosTipos.CambiarEstadoTipoAsync(1, 2, Admin, CancellationToken.None));

        var clase = await escenario.EstadosTipos.CrearClaseTipoAsync(
            new GuardarClaseTipoSolicitud { Nombre = "Operaciones", EstadoId = 1 }, Admin, CancellationToken.None);
        var claseActualizada = await escenario.EstadosTipos.ActualizarClaseTipoAsync(
            clase.Id, new GuardarClaseTipoSolicitud { Nombre = "Operativas", EstadoId = 1 }, Admin, CancellationToken.None);
        Assert.Equal("Operativas", claseActualizada.Nombre);
        Assert.Equal(2, (await escenario.EstadosTipos.CambiarEstadoClaseTipoAsync(
            clase.Id, 2, Admin, CancellationToken.None)).EstadoId);

        var tipo = await escenario.EstadosTipos.CrearTipoAsync(
            new GuardarTipoSolicitud { Nombre = "Mayoreo", EstadoId = 1, ClaseTipoId = clase.Id, ModuloId = "CONFIGURACION" },
            Admin,
            CancellationToken.None);
        var tipoActualizado = await escenario.EstadosTipos.ActualizarTipoAsync(
            tipo.Id,
            new GuardarTipoSolicitud { Nombre = "Minorista", EstadoId = 1, ClaseTipoId = clase.Id, ModuloId = "CONFIGURACION" },
            Admin,
            CancellationToken.None);
        Assert.Equal("Minorista", tipoActualizado.Nombre);
        Assert.Equal(2, (await escenario.EstadosTipos.CambiarEstadoTipoAsync(
            tipo.Id, 2, Admin, CancellationToken.None)).EstadoId);
    }

    [Fact]
    public async Task Modulo_NormalizaCodigoYProtegeEstructurales()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var creado = await escenario.Catalogos.CrearModuloAsync(
            new GuardarModuloSolicitud { Id = " ventas ", Nombre = "Ventas", Icono = "cart" }, Admin, CancellationToken.None);
        Assert.Equal("VENTAS", creado.Id);
        Assert.Equal("shopping-cart", (await escenario.Catalogos.ActualizarModuloAsync(
            creado.Id,
            new ActualizarModuloSolicitud { Nombre = "Ventas", Icono = "shopping-cart" },
            Admin,
            CancellationToken.None)).Icono);
        Assert.False((await escenario.Catalogos.CambiarActivoModuloAsync(
            creado.Id, false, Admin, CancellationToken.None)).Activo);
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Catalogos.CambiarActivoModuloAsync("SEGURIDAD", false, Admin, CancellationToken.None));
    }

    [Fact]
    public async Task AccionYNivel_ValidanIntegridadYActualizacion()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() => escenario.Catalogos.CrearAccionAsync(
            new GuardarAccionSistemaSolicitud { Codigo = "administrar", Nombre = "Duplicada", EstadoId = 1 }, Admin, CancellationToken.None));
        var accion = await escenario.Catalogos.CrearAccionAsync(
            new GuardarAccionSistemaSolicitud { Codigo = " consultar ", Nombre = "Consultar", EstadoId = 1 }, Admin, CancellationToken.None);
        Assert.Equal("CONSULTAR", accion.Codigo);
        Assert.Equal("Consulta", (await escenario.Catalogos.ActualizarAccionAsync(
            accion.Id,
            new GuardarAccionSistemaSolicitud { Codigo = "CONSULTAR", Nombre = "Consulta", EstadoId = 1 },
            Admin,
            CancellationToken.None)).Nombre);
        Assert.Equal(2, (await escenario.Catalogos.CambiarEstadoAccionAsync(
            accion.Id, 2, Admin, CancellationToken.None)).EstadoId);
        var nivel = await escenario.Catalogos.CrearNivelAsync(
            new GuardarNivelAccesoSolicitud { Nombre = "Operador" }, Admin, CancellationToken.None);
        Assert.Equal("Supervisor", (await escenario.Catalogos.ActualizarNivelAsync(
            nivel.Id, new GuardarNivelAccesoSolicitud { Nombre = "Supervisor" }, Admin, CancellationToken.None)).Nombre);
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() => escenario.Catalogos.ActualizarNivelAsync(
            1, new GuardarNivelAccesoSolicitud { Nombre = "Otro administrador" }, Admin, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() => escenario.Catalogos.CambiarEstadoAccionAsync(
            1, 2, Admin, CancellationToken.None));
    }

    [Fact]
    public async Task EmpresaYSucursal_RespetanAlcanceDeConsulta()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync(tienePermiso: false);
        var empresas = await escenario.Organizacion.ListarEmpresasAsync(new FiltroConEstadoSolicitud(), Consulta, CancellationToken.None);
        var sucursales = await escenario.Organizacion.ListarSucursalesAsync(new FiltroSucursalesSolicitud(), Consulta, CancellationToken.None);

        Assert.Single(empresas.Elementos);
        Assert.Equal(1, empresas.Elementos[0].Id);
        Assert.Single(sucursales.Elementos);
        Assert.Equal(1, sucursales.Elementos[0].Id);
        await Assert.ThrowsAsync<RecursoConfiguracionNoEncontradoException>(() =>
            escenario.Organizacion.ObtenerEmpresaAsync(2, Consulta, CancellationToken.None));
        await Assert.ThrowsAsync<RecursoConfiguracionNoEncontradoException>(() =>
            escenario.Organizacion.ObtenerSucursalAsync(2, Consulta, CancellationToken.None));
    }

    [Fact]
    public async Task Sucursal_RechazaProveedorDeOtraEmpresaYCentralDuplicada()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var solicitud = EscenarioConfiguracion.CrearSolicitudSucursal(proveedorId: 2);
        await Assert.ThrowsAsync<ValidacionConfiguracionException>(() =>
            escenario.Organizacion.CrearSucursalAsync(solicitud, Admin, CancellationToken.None));

        solicitud = EscenarioConfiguracion.CrearSolicitudSucursal(proveedorId: 1, esCentral: true);
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.CrearSucursalAsync(solicitud, Admin, CancellationToken.None));
    }

    [Fact]
    public async Task Empresa_PermiteCrearYActualizarSinExponerRoleId()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var solicitud = EscenarioConfiguracion.CrearSolicitudEmpresa("NIT-3", "tercera@pruebas.local");
        var creada = await escenario.Organizacion.CrearEmpresaAsync(solicitud, Admin, CancellationToken.None);
        Assert.Equal("ADMIN", (await escenario.Contexto.Empresas.SingleAsync(item => item.Id == creada.Id)).RoleId);

        var actualizada = await escenario.Organizacion.ActualizarEmpresaAsync(
            creada.Id,
            EscenarioConfiguracion.CrearSolicitudEmpresa("NIT-3", "tercera@pruebas.local", "Empresa actualizada"),
            Admin,
            CancellationToken.None);
        Assert.Equal("Empresa actualizada", actualizada.RazonSocial);
        Assert.Equal(2, (await escenario.Organizacion.CambiarEstadoEmpresaAsync(
            creada.Id, 2, Admin, CancellationToken.None)).EstadoId);
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() => escenario.Organizacion.CrearEmpresaAsync(
            EscenarioConfiguracion.CrearSolicitudEmpresa("NIT-1", "otra@pruebas.local"),
            Admin,
            CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.CambiarEstadoEmpresaAsync(1, 2, Admin, CancellationToken.None));
    }

    [Fact]
    public async Task EmpresaEstructural_ActualizacionAEstadoNoOperativoEsRechazada()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var solicitud = EscenarioConfiguracion.CrearSolicitudEmpresa(
            "NIT-1",
            "e1@p.local",
            "Empresa 1",
            estadoId: 2);

        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.ActualizarEmpresaAsync(
                1,
                solicitud,
                Admin,
                CancellationToken.None));

        Assert.Equal(1, (await escenario.Contexto.Empresas.SingleAsync(item => item.Id == 1)).EstadoId);
    }

    [Fact]
    public async Task EmpresaEstructural_ActualizacionSinCambiarEstadoEsPermitida()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var actualizada = await escenario.Organizacion.ActualizarEmpresaAsync(
            1,
            EscenarioConfiguracion.CrearSolicitudEmpresa("NIT-1", "e1@p.local", "Empresa principal actualizada"),
            Admin,
            CancellationToken.None);

        Assert.Equal("Empresa principal actualizada", actualizada.RazonSocial);
        Assert.Equal(1, actualizada.EstadoId);
    }

    [Fact]
    public async Task EmpresaInexistente_ActualizacionDevuelveNoEncontrado()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        await Assert.ThrowsAsync<RecursoConfiguracionNoEncontradoException>(() =>
            escenario.Organizacion.ActualizarEmpresaAsync(
                999,
                EscenarioConfiguracion.CrearSolicitudEmpresa("NIT-999", "noexiste@p.local"),
                Admin,
                CancellationToken.None));
    }

    [Fact]
    public async Task EmpresaNormal_ActualizarAEstadoNoOperativo_SoloModificaObjetivo()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var empresaEstructuralAntes = await ObtenerEmpresaAsync(escenario.Contexto, 1);
        var empresaNormalAntes = await ObtenerEmpresaAsync(escenario.Contexto, 2);

        var resultado = await escenario.Organizacion.ActualizarEmpresaAsync(
            2,
            EscenarioConfiguracion.CrearSolicitudEmpresa(
                "NIT-2",
                "e2@p.local",
                "Empresa 2 inactiva",
                estadoId: 2),
            Admin,
            CancellationToken.None);

        Assert.Equal(2, resultado.Id);
        Assert.Equal(2, resultado.EstadoId);
        Assert.Equal(empresaEstructuralAntes, await ObtenerEmpresaAsync(escenario.Contexto, 1));
        var empresaNormalDespues = await ObtenerEmpresaAsync(escenario.Contexto, 2);
        Assert.NotEqual(empresaNormalAntes, empresaNormalDespues);
        Assert.Equal("Empresa 2 inactiva", empresaNormalDespues.RazonSocial);
        Assert.Equal(2, empresaNormalDespues.EstadoId);
    }

    [Fact]
    public async Task EmpresaEstructural_InactivarNoModificaNingunaEmpresa()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var empresaEstructuralAntes = await ObtenerEmpresaAsync(escenario.Contexto, 1);
        var empresaNormalAntes = await ObtenerEmpresaAsync(escenario.Contexto, 2);

        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.ActualizarEmpresaAsync(
                1,
                EscenarioConfiguracion.CrearSolicitudEmpresa(
                    "NIT-1",
                    "e1@p.local",
                    "Empresa estructural alterada",
                    estadoId: 2),
                Admin,
                CancellationToken.None));

        Assert.Equal(empresaEstructuralAntes, await ObtenerEmpresaAsync(escenario.Contexto, 1));
        Assert.Equal(empresaNormalAntes, await ObtenerEmpresaAsync(escenario.Contexto, 2));
    }

    [Fact]
    public async Task EmpresaNormal_ActualizarCamposSoloModificaObjetivo()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var empresaEstructuralAntes = await ObtenerEmpresaAsync(escenario.Contexto, 1);

        await escenario.Organizacion.ActualizarEmpresaAsync(
            2,
            EscenarioConfiguracion.CrearSolicitudEmpresa(
                "NIT-2",
                "e2@p.local",
                "Empresa 2 actualizada"),
            Admin,
            CancellationToken.None);

        Assert.Equal(empresaEstructuralAntes, await ObtenerEmpresaAsync(escenario.Contexto, 1));
        Assert.Equal("Empresa 2 actualizada", (await ObtenerEmpresaAsync(escenario.Contexto, 2)).RazonSocial);
    }

    [Fact]
    public async Task EmpresaEstructural_ActualizarCamposSinCambiarEstadoSoloModificaObjetivo()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var empresaNormalAntes = await ObtenerEmpresaAsync(escenario.Contexto, 2);

        await escenario.Organizacion.ActualizarEmpresaAsync(
            1,
            EscenarioConfiguracion.CrearSolicitudEmpresa(
                "NIT-1",
                "e1@p.local",
                "Empresa estructural actualizada"),
            Admin,
            CancellationToken.None);

        var empresaEstructuralDespues = await ObtenerEmpresaAsync(escenario.Contexto, 1);
        Assert.Equal("Empresa estructural actualizada", empresaEstructuralDespues.RazonSocial);
        Assert.Equal(1, empresaEstructuralDespues.EstadoId);
        Assert.Equal(empresaNormalAntes, await ObtenerEmpresaAsync(escenario.Contexto, 2));
    }

    [Fact]
    public async Task EmpresaInexistente_NoModificaNingunaEmpresa()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var empresaEstructuralAntes = await ObtenerEmpresaAsync(escenario.Contexto, 1);
        var empresaNormalAntes = await ObtenerEmpresaAsync(escenario.Contexto, 2);

        await Assert.ThrowsAsync<RecursoConfiguracionNoEncontradoException>(() =>
            escenario.Organizacion.ActualizarEmpresaAsync(
                999,
                EscenarioConfiguracion.CrearSolicitudEmpresa("NIT-999", "noexiste@p.local"),
                Admin,
                CancellationToken.None));

        Assert.Equal(empresaEstructuralAntes, await ObtenerEmpresaAsync(escenario.Contexto, 1));
        Assert.Equal(empresaNormalAntes, await ObtenerEmpresaAsync(escenario.Contexto, 2));
    }

    [Fact]
    public async Task Empresas_DosActualizacionesConsecutivasMismoDbContext_NoCruzanEntidades()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();

        await escenario.Organizacion.ActualizarEmpresaAsync(
            2,
            EscenarioConfiguracion.CrearSolicitudEmpresa(
                "NIT-2",
                "e2@p.local",
                "Empresa normal primero"),
            Admin,
            CancellationToken.None);
        var empresaNormalTrasPrimeraOperacion = await ObtenerEmpresaAsync(escenario.Contexto, 2);

        await escenario.Organizacion.ActualizarEmpresaAsync(
            1,
            EscenarioConfiguracion.CrearSolicitudEmpresa(
                "NIT-1",
                "e1@p.local",
                "Empresa estructural después"),
            Admin,
            CancellationToken.None);

        Assert.Equal("Empresa estructural después", (await ObtenerEmpresaAsync(escenario.Contexto, 1)).RazonSocial);
        Assert.Equal(empresaNormalTrasPrimeraOperacion, await ObtenerEmpresaAsync(escenario.Contexto, 2));
    }

    [Fact]
    public async Task Empresas_CambiarEstadoSoloAfectaObjetivoYProtegeEstructural()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var empresaEstructuralAntes = await ObtenerEmpresaAsync(escenario.Contexto, 1);
        var empresaNormalAntes = await ObtenerEmpresaAsync(escenario.Contexto, 2);

        await escenario.Organizacion.CambiarEstadoEmpresaAsync(
            2,
            2,
            Admin,
            CancellationToken.None);

        Assert.Equal(empresaEstructuralAntes, await ObtenerEmpresaAsync(escenario.Contexto, 1));
        var empresaNormalInactiva = await ObtenerEmpresaAsync(escenario.Contexto, 2);
        Assert.Equal(empresaNormalAntes with { EstadoId = 2 }, empresaNormalInactiva);

        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.CambiarEstadoEmpresaAsync(
                1,
                2,
                Admin,
                CancellationToken.None));

        Assert.Equal(empresaEstructuralAntes, await ObtenerEmpresaAsync(escenario.Contexto, 1));
        Assert.Equal(empresaNormalInactiva, await ObtenerEmpresaAsync(escenario.Contexto, 2));
    }

    [Fact]
    public async Task Sucursales_ActualizarSoloAfectaObjetivoYProtegeEstructural()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var sucursalEstructuralAntes = await ObtenerSucursalAsync(escenario.Contexto, 1);

        await escenario.Organizacion.ActualizarSucursalAsync(
            2,
            EscenarioConfiguracion.CrearSolicitudSucursal(
                2,
                esCentral: true,
                nombre: "Sucursal 2 actualizada",
                estadoId: 2,
                email: "s2@p.local",
                codigo: "S2",
                empresaId: 2),
            Admin,
            CancellationToken.None);

        Assert.Equal(sucursalEstructuralAntes, await ObtenerSucursalAsync(escenario.Contexto, 1));
        var sucursalNormalInactiva = await ObtenerSucursalAsync(escenario.Contexto, 2);
        Assert.Equal("Sucursal 2 actualizada", sucursalNormalInactiva.Nombre);
        Assert.Equal(2, sucursalNormalInactiva.EstadoId);

        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.ActualizarSucursalAsync(
                1,
                EscenarioConfiguracion.CrearSolicitudSucursal(
                    1,
                    esCentral: true,
                    nombre: "Sucursal estructural alterada",
                    estadoId: 2,
                    email: "s1@p.local",
                    codigo: "S1"),
                Admin,
                CancellationToken.None));

        Assert.Equal(sucursalEstructuralAntes, await ObtenerSucursalAsync(escenario.Contexto, 1));
        Assert.Equal(sucursalNormalInactiva, await ObtenerSucursalAsync(escenario.Contexto, 2));
    }

    [Fact]
    public async Task Sucursales_CambiarEstadoSoloAfectaObjetivoYProtegeEstructural()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var sucursalEstructuralAntes = await ObtenerSucursalAsync(escenario.Contexto, 1);
        var sucursalNormalAntes = await ObtenerSucursalAsync(escenario.Contexto, 2);

        await escenario.Organizacion.CambiarEstadoSucursalAsync(
            2,
            2,
            Admin,
            CancellationToken.None);

        Assert.Equal(sucursalEstructuralAntes, await ObtenerSucursalAsync(escenario.Contexto, 1));
        var sucursalNormalInactiva = await ObtenerSucursalAsync(escenario.Contexto, 2);
        Assert.Equal(sucursalNormalAntes with { EstadoId = 2 }, sucursalNormalInactiva);

        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.CambiarEstadoSucursalAsync(
                1,
                2,
                Admin,
                CancellationToken.None));

        Assert.Equal(sucursalEstructuralAntes, await ObtenerSucursalAsync(escenario.Contexto, 1));
        Assert.Equal(sucursalNormalInactiva, await ObtenerSucursalAsync(escenario.Contexto, 2));
    }

    [Fact]
    public async Task Sucursal_PermiteCrearActualizarFiltrarYCambiarEstado()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var creada = await escenario.Organizacion.CrearSucursalAsync(
            EscenarioConfiguracion.CrearSolicitudSucursal(1), Admin, CancellationToken.None);
        Assert.Equal(1, creada.EmpresaId);
        Assert.Equal("NUEVA Nueva", creada.CodigoNombre);

        var actualizada = await escenario.Organizacion.ActualizarSucursalAsync(
            creada.Id,
            EscenarioConfiguracion.CrearSolicitudSucursal(1, nombre: "Zona 10"),
            Admin,
            CancellationToken.None);
        Assert.Equal("Zona 10", actualizada.Nombre);
        Assert.Equal(2, (await escenario.Organizacion.CambiarEstadoSucursalAsync(
            creada.Id, 2, Admin, CancellationToken.None)).EstadoId);
        Assert.Equal(creada.Id, (await escenario.Organizacion.ObtenerSucursalAsync(
            creada.Id, Admin, CancellationToken.None)).Id);

        var filtro = await escenario.Organizacion.ListarSucursalesAsync(
            new FiltroSucursalesSolicitud { EmpresaId = 2 }, Admin, CancellationToken.None);
        Assert.Single(filtro.Elementos);
        Assert.Equal(2, filtro.Elementos[0].EmpresaId);
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.CambiarEstadoSucursalAsync(1, 2, Admin, CancellationToken.None));
        await Assert.ThrowsAsync<ConflictoConfiguracionException>(() =>
            escenario.Organizacion.ActualizarSucursalAsync(
                1,
                EscenarioConfiguracion.CrearSolicitudSucursal(
                    1,
                    esCentral: true,
                    nombre: "Central actualizada",
                    estadoId: 2,
                    email: "s1@p.local",
                    codigo: "S1"),
                Admin,
                CancellationToken.None));
    }

    [Fact]
    public async Task AuditoriaReal_RegistraCambioSinCopiarValoresSensibles()
    {
        await using var escenario = await EscenarioConfiguracion.CrearAsync();
        var servicio = new ServicioCatalogosConfiguracion(
            escenario.Contexto,
            new AuditoriaConfiguracion(escenario.Contexto, TimeProvider.System));

        await servicio.ActualizarModuloAsync(
            "CONFIGURACION",
            new ActualizarModuloSolicitud { Nombre = "Configuración", Icono = "gear" },
            Admin,
            CancellationToken.None);

        var registro = await escenario.Contexto.Bitacoras.SingleAsync();
        Assert.Equal("Modulos", registro.Entidad);
        Assert.Equal("ACTUALIZAR", registro.Accion);
        Assert.Equal("CONFIGURACION", registro.ModuloId);
        Assert.Null(registro.ValorAnterior);
        Assert.Null(registro.ValorNuevo);
    }

    private static async Task<EmpresaInstantanea> ObtenerEmpresaAsync(
        HeladeriaDbContext contexto,
        int id)
    {
        var empresa = await contexto.Empresas.AsNoTracking().SingleAsync(item => item.Id == id);
        return new(
            empresa.Id,
            empresa.RazonSocial,
            empresa.Eslogan,
            empresa.Direccion,
            empresa.Nit,
            empresa.Dpi,
            empresa.Telefono,
            empresa.Email,
            empresa.Imagen,
            empresa.Icono,
            empresa.EstadoId,
            empresa.ImagenReportes,
            empresa.RoleId,
            empresa.ClienteId,
            empresa.NombreComercial,
            empresa.Url);
    }

    private static async Task<SucursalInstantanea> ObtenerSucursalAsync(
        HeladeriaDbContext contexto,
        long id)
    {
        var sucursal = await contexto.Sucursales.AsNoTracking().SingleAsync(item => item.Id == id);
        return new(
            sucursal.Id,
            sucursal.Nombre,
            sucursal.Direccion,
            sucursal.Telefono,
            sucursal.Email,
            sucursal.EmpresaId,
            sucursal.LugarId,
            sucursal.MunicipioId,
            sucursal.DepartamentoId,
            sucursal.EsCentral,
            sucursal.ClienteId,
            sucursal.EstadoId,
            sucursal.ProveedorId,
            sucursal.EsBodega,
            sucursal.SucursalCentralId,
            sucursal.Imagen,
            sucursal.SucursalTransporteId,
            sucursal.RepresentanteLegal,
            sucursal.DireccionCorta,
            sucursal.Codigo,
            sucursal.ImagenReportes,
            sucursal.Transportes,
            sucursal.CodigoNombre);
    }
}

internal sealed record EmpresaInstantanea(
    int Id,
    string RazonSocial,
    string Eslogan,
    string Direccion,
    string Nit,
    string? Dpi,
    string Telefono,
    string Email,
    string Imagen,
    string Icono,
    int EstadoId,
    string ImagenReportes,
    string RoleId,
    int? ClienteId,
    string? NombreComercial,
    string? Url);

internal sealed record SucursalInstantanea(
    long Id,
    string Nombre,
    string Direccion,
    string Telefono,
    string Email,
    int EmpresaId,
    int? LugarId,
    int? MunicipioId,
    int? DepartamentoId,
    bool EsCentral,
    int? ClienteId,
    int EstadoId,
    int ProveedorId,
    bool EsBodega,
    long? SucursalCentralId,
    string? Imagen,
    long? SucursalTransporteId,
    string? RepresentanteLegal,
    string DireccionCorta,
    string? Codigo,
    string? ImagenReportes,
    bool Transportes,
    string CodigoNombre);

internal sealed class EscenarioConfiguracion : IAsyncDisposable
{
    private EscenarioConfiguracion(HeladeriaDbContext contexto, bool tienePermiso)
    {
        Contexto = contexto;
        Auditoria = new AuditoriaConfiguracionFalsa();
        EstadosTipos = new ServicioEstadosTiposConfiguracion(contexto, Auditoria);
        Catalogos = new ServicioCatalogosConfiguracion(contexto, Auditoria);
        Organizacion = new ServicioOrganizacionConfiguracion(
            contexto,
            Auditoria,
            new ServicioPermisosConfiguracionFalso(tienePermiso),
            Options.Create(new OpcionesSeguridad()));
    }

    public HeladeriaDbContext Contexto { get; }
    public AuditoriaConfiguracionFalsa Auditoria { get; }
    public ServicioEstadosTiposConfiguracion EstadosTipos { get; }
    public ServicioCatalogosConfiguracion Catalogos { get; }
    public ServicioOrganizacionConfiguracion Organizacion { get; }

    public static async Task<EscenarioConfiguracion> CrearAsync(bool tienePermiso = true)
    {
        var opciones = new DbContextOptionsBuilder<HeladeriaDbContext>()
            .UseInMemoryDatabase($"configuracion-{Guid.NewGuid():N}").Options;
        var escenario = new EscenarioConfiguracion(new HeladeriaDbContext(opciones), tienePermiso);
        await escenario.SemillarAsync();
        return escenario;
    }

    public ValueTask DisposeAsync() => Contexto.DisposeAsync();

    public static GuardarEmpresaSolicitud CrearSolicitudEmpresa(
        string nit,
        string email,
        string razonSocial = "Empresa nueva",
        int estadoId = 1) => new()
        {
            RazonSocial = razonSocial,
            NombreComercial = razonSocial,
            Eslogan = "Eslogan",
            Direccion = "Dirección",
            Nit = nit,
            Telefono = "5555",
            Email = email,
            Imagen = "sin-imagen",
            Icono = "sin-icono",
            ImagenReportes = "sin-imagen",
            EstadoId = estadoId
        };

    public static GuardarSucursalSolicitud CrearSolicitudSucursal(
        int proveedorId,
        bool esCentral = false,
        string nombre = "Nueva",
        int estadoId = 1,
        string email = "nueva@pruebas.local",
        string codigo = "NUEVA",
        int empresaId = 1) => new()
        {
            Nombre = nombre,
            Direccion = "Dirección",
            DireccionCorta = "Corta",
            Telefono = "5555",
            Email = email,
            EmpresaId = empresaId,
            EstadoId = estadoId,
            ProveedorId = proveedorId,
            EsCentral = esCentral,
            Codigo = codigo
        };

    private async Task SemillarAsync()
    {
        Contexto.AddRange(
            new ClaseEstados { Id = 1, Nombre = "Seguridad", Activo = true },
            new Estados { Id = 1, Nombre = "Activo", Activo = true, ClaseEstadoId = 1 },
            new Estados { Id = 2, Nombre = "Inactivo", Activo = true, ClaseEstadoId = 1 },
            new Modulos { Id = "SEGURIDAD", Nombre = "Seguridad", Icono = "shield", Activo = true },
            new Modulos { Id = "CONFIGURACION", Nombre = "Configuración", Icono = "settings", Activo = true },
            new ClaseTipos { Id = 1, Nombre = "Seguridad", EstadoId = 1 },
            new Tipos { Id = 1, Nombre = "Inicio local", EstadoId = 1, ClaseTipoId = 1, ModuloId = "SEGURIDAD" },
            new NivelesDeAccesos { Id = 1, Nombre = "Administrador" },
            new Roles { Id = "ADMIN", Nombre = "Administrador", NivelDeAccesoId = 1, CambioDeContrasenia = 12, TipoId = 1, AplicarConfiguracionesPorRol = true },
            new Roles { Id = "CONSULTA", Nombre = "Consulta", NivelDeAccesoId = 1, CambioDeContrasenia = 12, TipoId = 1 },
            new AccionesSistema { Id = 1, Codigo = "ADMINISTRAR", Nombre = "Administrar", EstadoId = 1 },
            new Permisos { Id = 1, ModuloId = "CONFIGURACION", AccionId = 1, Recurso = "Configuración", Codigo = PermisosConfiguracion.Administrar, EstadoId = 1 },
            new Permisos { Id = 2, ModuloId = "SEGURIDAD", AccionId = 1, Recurso = "Seguridad", Codigo = "SEGURIDAD.ADMINISTRAR", EstadoId = 1 },
            new RolesPermisos { Id = 1, RoleId = "ADMIN", PermisoId = 1, Permitido = true },
            new RolesPermisos { Id = 2, RoleId = "ADMIN", PermisoId = 2, Permitido = true },
            new Empresas { Id = 1, RazonSocial = "Empresa 1", NombreComercial = "E1", Eslogan = "E", Direccion = "D", Nit = "NIT-1", Telefono = "1", Email = "e1@p.local", Imagen = "i", Icono = "i", ImagenReportes = "i", EstadoId = 1, RoleId = "ADMIN" },
            new Empresas { Id = 2, RazonSocial = "Empresa 2", NombreComercial = "E2", Eslogan = "E", Direccion = "D", Nit = "NIT-2", Telefono = "2", Email = "e2@p.local", Imagen = "i", Icono = "i", ImagenReportes = "i", EstadoId = 1, RoleId = "ADMIN" },
            new Proveedores { Id = 1, EmpresaId = 1, CodigoProveedor = "P1", NombreProveedor = "Proveedor 1", DiasCredito = 0, EstadoId = 1 },
            new Proveedores { Id = 2, EmpresaId = 2, CodigoProveedor = "P2", NombreProveedor = "Proveedor 2", DiasCredito = 0, EstadoId = 1 },
            CrearSucursal(1, 1, 1, "Central 1", true),
            CrearSucursal(2, 2, 2, "Central 2", true),
            CrearUsuario(1, "admin", "ADMIN"),
            CrearUsuario(2, "consulta", "CONSULTA"),
            new UsuariosSucursales { Id = 1, UsuarioId = 1, SucursalId = 1, EsPrincipal = true, EstadoId = 1 },
            new UsuariosSucursales { Id = 2, UsuarioId = 2, SucursalId = 1, EsPrincipal = true, EstadoId = 1 });
        await Contexto.SaveChangesAsync();
        Contexto.ChangeTracker.Clear();
    }

    private static Sucursales CrearSucursal(long id, int empresaId, int proveedorId, string nombre, bool central) => new()
    {
        Id = id,
        Nombre = nombre,
        Direccion = "D",
        DireccionCorta = "D",
        Telefono = "1",
        Email = $"s{id}@p.local",
        EmpresaId = empresaId,
        EsCentral = central,
        EstadoId = 1,
        ProveedorId = proveedorId,
        Codigo = $"S{id}",
        CodigoNombre = $"S{id} {nombre}"
    };

    private static Usuarios CrearUsuario(int id, string nombre, string rol) => new()
    {
        Id = id,
        Email = $"{nombre}@p.local",
        UserName = nombre,
        IdentityId = nombre,
        PasswordHash = "hash",
        RoleId = rol,
        HorarioInicio = new TimeOnly(),
        HorarioFinal = new TimeOnly(),
        Imagen = "i",
        Soundex = "S",
        EstadoId = 1,
        TipoInicioSesionId = 1
    };
}

internal sealed class AuditoriaConfiguracionFalsa : IAuditoriaConfiguracion
{
    public int Cantidad { get; private set; }
    public void Registrar(ContextoConfiguracion contexto, string entidad, string? entidadId, string accion, string comentario) => Cantidad++;
}

internal sealed class ServicioPermisosConfiguracionFalso(bool permitido) : IServicioPermisos
{
    public Task<bool> TienePermisoAsync(int usuarioId, string rolId, string codigoPermiso, CancellationToken cancellationToken) => Task.FromResult(permitido);
    public Task<IReadOnlyList<string>> ObtenerPermisosAsync(int usuarioId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>(permitido ? [PermisosConfiguracion.Administrar] : []);
}
