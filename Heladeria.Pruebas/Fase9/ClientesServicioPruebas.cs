using Heladeria.Aplicacion.Clientes;
using Heladeria.Infraestructura.ClientesGestion;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Heladeria.Pruebas.Fase5;
using Heladeria.Pruebas.Fase8;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Pruebas.Fase9;

public sealed class ClientesServicioPruebas
{
    [Fact]
    public async Task DatosMinimosActivosYClaveNaturalEsCodigoPorEmpresaNoNitNiCorreo()
    {
        await using var e = await EscenarioCaja.CrearAsync(); var servicio = Servicio(e);
        e.Contexto.Clientes.Add(new Clientes { EmpresaId = 2, CodigoCliente = "MINIMO", NombreCliente = "Ajeno", EstadoId = 1 });
        await e.Contexto.SaveChangesAsync();
        var minimo = await servicio.CrearAsync(new() { CodigoCliente = "MINIMO", NombreCliente = "Mínimo" }, EscenarioCaja.Seguridad, default);
        Assert.Equal(0, minimo.LimiteCredito); Assert.Equal(0, minimo.DiasCredito); Assert.Null(minimo.Nit); Assert.Null(minimo.Correo);
        foreach (var codigo in new[] { "OTRO-1", "OTRO-2" })
            await servicio.CrearAsync(new() { CodigoCliente = codigo, NombreCliente = "Otro", Nit = "123", Correo = "compartido@example.test" }, EscenarioCaja.Seguridad, default);
        Assert.Equal(3, (await servicio.ListarAsync(new(), EscenarioCaja.Seguridad, default)).TotalRegistros);
        Assert.Equal(4, await e.Contexto.Clientes.CountAsync());
    }

    [Fact]
    public async Task EstadoSemanticoIdNoAsumidoFiltroYReferenciasSoloOperativas()
    {
        await using var e = await EscenarioCaja.CrearAsync();
        // El activo original sigue válido para el contexto pero pertenece a otro módulo.
        (await e.Contexto.Estados.SingleAsync(x => x.Id == 1)).ModuloId = "SEGURIDAD";
        e.Contexto.Estados.Add(new Estados { Id = 87, Nombre = "Activo", Activo = true, ClaseEstadoId = 1 });
        e.Contexto.Estados.Add(new Estados { Id = 86, Nombre = "Activo", Activo = false, ClaseEstadoId = 1 });
        await e.Contexto.SaveChangesAsync();
        var servicio = Servicio(e);
        var c = await servicio.CrearAsync(new() { CodigoCliente = "ACTIVO", NombreCliente = "Activo" }, EscenarioCaja.Seguridad, default);
        Assert.Equal(87, c.EstadoId); Assert.Equal("Activo", c.Estado);
        e.Contexto.Clientes.Add(new Clientes { EmpresaId = 1, CodigoCliente = "INACTIVO", NombreCliente = "Inactivo", EstadoId = 2 });
        await e.Contexto.SaveChangesAsync();
        var inactivos = await servicio.ListarAsync(new() { EstadoId = 2 }, EscenarioCaja.Seguridad, default);
        Assert.Equal("INACTIVO", Assert.Single(inactivos.Elementos).CodigoCliente);
        var referencias = await e.Ventas.Ventas.ReferenciasAsync(EscenarioCaja.Seguridad, default);
        Assert.Equal(c.Id, Assert.Single(referencias.Clientes).Id);
    }

    [Fact]
    public async Task SinEstadoOperativoCompatibleNoCreaClienteNiAuditoria()
    {
        await using var e = await EscenarioCaja.CrearAsync();
        (await e.Contexto.Estados.SingleAsync(x => x.Id == 1)).ModuloId = "SEGURIDAD";
        await e.Contexto.SaveChangesAsync(); var antes = await e.Contexto.Bitacoras.CountAsync();
        var error = await Assert.ThrowsAsync<ExcepcionClientes>(() => Servicio(e).CrearAsync(new() {
            CodigoCliente = "NUEVO", NombreCliente = "Cliente" }, EscenarioCaja.Seguridad, default));
        Assert.Equal("ESTADO_CLIENTE_NO_DISPONIBLE", error.Codigo); Assert.Equal(409, error.EstadoHttp);
        Assert.Empty(e.Contexto.Clientes); Assert.Equal(antes, await e.Contexto.Bitacoras.CountAsync());
    }

    [Theory]
    [InlineData("usuario")]
    [InlineData("empresa")]
    [InlineData("sucursal")]
    [InlineData("asignacion")]
    [InlineData("empresa-ajena")]
    [InlineData("sucursal-no-asignada")]
    public async Task ContextoInoperativoONoAutorizadoSeRechazaEnAltaYConsultas(string caso)
    {
        await using var e = await EscenarioCaja.CrearAsync(); var s = EscenarioCaja.Seguridad;
        if (caso == "usuario") (await e.Contexto.Usuarios.SingleAsync(x => x.Id == s.UsuarioId)).EstadoId = 2;
        if (caso == "empresa") (await e.Contexto.Empresas.SingleAsync(x => x.Id == s.EmpresaId)).EstadoId = 2;
        if (caso == "sucursal") (await e.Contexto.Sucursales.SingleAsync(x => x.Id == s.SucursalId)).EstadoId = 2;
        if (caso == "asignacion") (await e.Contexto.UsuariosSucursales.SingleAsync(x => x.UsuarioId == s.UsuarioId && x.SucursalId == s.SucursalId)).EstadoId = 2;
        if (caso == "empresa-ajena") s = s with { EmpresaId = 2 };
        if (caso == "sucursal-no-asignada") s = s with { SucursalId = 4 };
        await e.Contexto.SaveChangesAsync(); var servicio = Servicio(e);
        Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionClientes>(() => servicio.CrearAsync(new() {
            CodigoCliente = "NUEVO", NombreCliente = "Cliente" }, s, default))).EstadoHttp);
        Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionClientes>(() => servicio.ListarAsync(new(), s, default))).EstadoHttp);
        Assert.Equal(403, (await Assert.ThrowsAsync<ExcepcionClientes>(() => servicio.ObtenerAsync(1, s, default))).EstadoHttp);
        Assert.Empty(e.Contexto.Clientes);
    }

    [Fact]
    public async Task FalloDeAuditoriaRevierteClienteYPermiteReintento()
    {
        await using var e = await EscenarioCaja.CrearAsync(); var db = e.Contexto; var servicio = Servicio(e);
        var antes = await db.Bitacoras.CountAsync();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER FalloAuditoriaCliente BEFORE INSERT ON Bitacoras WHEN NEW.Accion='CLIENTE_CREADO'
            BEGIN SELECT RAISE(ABORT, 'FALLO_AUDITORIA_CLIENTE'); END;
            """);
        var solicitud = new CrearClienteSolicitud { CodigoCliente = "REINTENTO", NombreCliente = "Cliente" };
        await Assert.ThrowsAsync<DbUpdateException>(() => servicio.CrearAsync(solicitud, EscenarioCaja.Seguridad, default));
        Assert.Empty(db.Clientes); Assert.Equal(antes, await db.Bitacoras.CountAsync()); Assert.False(db.ChangeTracker.HasChanges());
        await db.Database.ExecuteSqlRawAsync("DROP TRIGGER FalloAuditoriaCliente;");
        var creado = await servicio.CrearAsync(solicitud, EscenarioCaja.Seguridad, default);
        Assert.Equal(creado.Id, (await db.Clientes.SingleAsync()).Id); Assert.Equal(antes + 1, await db.Bitacoras.CountAsync());
    }

    private static ServicioClientes Servicio(EscenarioCaja e) => new(e.Contexto, EscenarioFase5.Opciones, TimeProvider.System);
}
