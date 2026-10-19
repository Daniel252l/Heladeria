using System.ComponentModel.DataAnnotations;
using System.Data;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Inventario;
using Heladeria.Infraestructura.Persistencia;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Heladeria.Infraestructura.Produccion;

public abstract class ServicioProduccionBase(
    HeladeriaDbContext contexto,
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider reloj,
    CoordinadorAjustesInventario coordinador,
    ServicioInventario inventario)
{
    protected HeladeriaDbContext Contexto { get; } = contexto;
    protected string[] Operativos { get; } = opciones.Value.EstadosActivos;
    protected OpcionesSeguridad ConfiguracionTiempo { get; } = opciones.Value;
    protected DateTime Ahora => reloj.GetUtcNow().UtcDateTime;
    protected ServicioInventario Inventario { get; } = inventario;
    protected const decimal MaximoCantidad = 9999999999999.999999m;

    protected static string Codigo(string valor)
    {
        var resultado = valor.Trim().ToUpperInvariant();
        if (resultado.Length == 0)
            throw new ValidacionProduccionException("Los textos obligatorios no pueden contener solamente espacios.");
        return resultado;
    }

    protected static string Texto(string valor)
    {
        var resultado = valor.Trim();
        if (resultado.Length == 0)
            throw new ValidacionProduccionException("Los textos obligatorios no pueden contener solamente espacios.");
        return resultado;
    }

    protected static string? Opcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    protected static void Validar(object solicitud)
    {
        var errores = new List<ValidationResult>();
        if (!Validator.TryValidateObject(solicitud, new ValidationContext(solicitud), errores, true))
            throw new ValidacionProduccionException(string.Join(" ", errores.Select(item => item.ErrorMessage)));
    }

    protected static void Precision(decimal valor, string nombre, bool positivo = true)
    {
        if (valor < 0 || positivo && valor == 0 || valor > MaximoCantidad
            || valor != decimal.Round(valor, 6))
            throw new ValidacionProduccionException($"{nombre} no tiene un valor o precisión válidos para decimal(19,6).");
    }

    protected static decimal Multiplicar(decimal izquierda, decimal derecha, string nombre)
    {
        decimal valor;
        try { valor = izquierda * derecha; }
        catch (OverflowException) { throw new ValidacionProduccionException($"{nombre} excede la capacidad admitida."); }
        Precision(valor, nombre);
        return valor;
    }

    protected static string CodificarVersion(byte[]? version) => Convert.ToBase64String(version ?? []);

    protected static void ValidarVersion(byte[]? actual, string solicitada)
    {
        byte[] recibida;
        try { recibida = Convert.FromBase64String(solicitada); }
        catch (FormatException) { throw new ValidacionProduccionException("VersionFila no tiene un formato Base64 válido."); }
        if (!(actual ?? []).AsSpan().SequenceEqual(recibida))
            throw new ConflictoProduccionException("El registro fue modificado por otro usuario. Recargue los datos.");
    }

    protected async Task ValidarSucursalAsync(ContextoConfiguracion seguridad, CancellationToken token)
    {
        if (!await Contexto.UsuariosSucursales.AsNoTracking().AnyAsync(item =>
            item.UsuarioId == seguridad.UsuarioId && item.SucursalId == seguridad.SucursalId
            && item.Sucursal.EmpresaId == seguridad.EmpresaId
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.Sucursal.Estado.Activo && Operativos.Contains(item.Sucursal.Estado.Nombre)
            && item.Sucursal.Empresa.Estado.Activo && Operativos.Contains(item.Sucursal.Empresa.Estado.Nombre), token))
            throw new RecursoProduccionNoEncontradoException("La sucursal no existe dentro del contexto autorizado.");
    }

    protected async Task<ProductoProduccion> ValidarProductoAsync(
        long productoId, ContextoConfiguracion seguridad, CancellationToken token)
    {
        var producto = await Contexto.Productos.AsNoTracking()
            .Where(item => item.Id == productoId && item.EmpresaId == seguridad.EmpresaId
                && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
                && item.TiposProductos.EmpresaId == seguridad.EmpresaId
                && item.TiposProductos.ManejaExistencia
                && item.TiposProductos.Estado.Activo && Operativos.Contains(item.TiposProductos.Estado.Nombre)
                && item.UnidadesMedida.EmpresaId == seguridad.EmpresaId
                && item.UnidadesMedida.Estado.Activo && Operativos.Contains(item.UnidadesMedida.Estado.Nombre)
                && item.TipoSeguimiento.ClaseCatalogo.Codigo == "TIPO_SEGUIMIENTO_PRODUCTO"
                && (item.TipoSeguimiento.EmpresaId == null || item.TipoSeguimiento.EmpresaId == seguridad.EmpresaId)
                && item.TipoSeguimiento.Estado.Activo && Operativos.Contains(item.TipoSeguimiento.Estado.Nombre)
                && item.TipoSeguimiento.ClaseCatalogo.Estado.Activo
                && Operativos.Contains(item.TipoSeguimiento.ClaseCatalogo.Estado.Nombre)
                && item.MetodoValuacion.ClaseCatalogo.Codigo == "METODO_VALUACION_INVENTARIO"
                && (item.MetodoValuacion.EmpresaId == null || item.MetodoValuacion.EmpresaId == seguridad.EmpresaId)
                && item.MetodoValuacion.Estado.Activo && Operativos.Contains(item.MetodoValuacion.Estado.Nombre)
                && item.MetodoValuacion.ClaseCatalogo.Estado.Activo
                && Operativos.Contains(item.MetodoValuacion.ClaseCatalogo.Estado.Nombre))
            .Select(item => new ProductoProduccion(item.Id, item.Nombre, item.UnidadBaseId,
                item.UnidadesMedida.Nombre, item.UnidadesMedida.Abreviatura,
                item.TipoSeguimiento.Codigo, item.MetodoValuacion.Codigo, item.ControlaVencimiento))
            .SingleOrDefaultAsync(token)
            ?? throw new ValidacionProduccionException(
                "El producto no pertenece a la empresa, está inactivo o no maneja existencia.");

        if (producto.ControlaVencimiento || producto.TipoSeguimiento != "NINGUNO"
            || producto.MetodoValuacion != "PROMEDIO")
            throw new ConflictoProduccionException(
                "Fase 6 admite productos sin lote/serie/vencimiento y con valoración PROMEDIO.");
        return producto;
    }

    protected async Task<ConversionProduccion> ConvertirAsync(
        long productoId, long? presentacionId, decimal cantidad,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        try
        {
            var conversion = await Inventario.ConvertirCantidadAsync(
                productoId, presentacionId, cantidad, seguridad.EmpresaId, token);
            return new(conversion.UnidadEntradaId, conversion.Factor, conversion.CantidadBase);
        }
        catch (ValidacionFase4Exception error)
        {
            throw new ValidacionProduccionException(error.Message);
        }
    }

    protected async Task ValidarPresentacionAsync(
        long productoId, long? presentacionId,
        ContextoConfiguracion seguridad, CancellationToken token)
    {
        if (!presentacionId.HasValue)
            return;
        if (!await Contexto.ProductoPresentaciones.AsNoTracking().AnyAsync(item =>
            item.Id == presentacionId && item.ProductoId == productoId
            && item.EmpresaId == seguridad.EmpresaId
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre)
            && item.Presentaciones.EmpresaId == seguridad.EmpresaId
            && item.Presentaciones.Estado.Activo && Operativos.Contains(item.Presentaciones.Estado.Nombre)
            && item.UnidadesMedida.EmpresaId == seguridad.EmpresaId
            && item.UnidadesMedida.Estado.Activo && Operativos.Contains(item.UnidadesMedida.Estado.Nombre), token))
            throw new ValidacionProduccionException(
                "La presentación no pertenece al producto o no está operativa.");
    }

    protected async Task<EstadosFlujo> EstadoFlujoAsync(string codigo, CancellationToken token) =>
        await Contexto.EstadosFlujo.AsNoTracking().SingleOrDefaultAsync(item =>
            item.FlujoEstado.Codigo == ServicioProduccion.FlujoOrden
            && item.Codigo == codigo
            && item.FlujoEstado.Estado.Activo && Operativos.Contains(item.FlujoEstado.Estado.Nombre)
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token)
        ?? throw new ConflictoProduccionException(
            "Faltan estados operativos de Fase 6; ejecute la preparación explícita.");

    protected async Task TransicionarAsync(
        OrdenesProduccion orden, string destino, string permiso, CancellationToken token)
    {
        var estado = await EstadoFlujoAsync(destino, token);
        if (!await Contexto.EstadoTransiciones.AsNoTracking().AnyAsync(item =>
            item.FlujoEstadoId == orden.FlujoEstadoId
            && item.EstadoOrigenId == orden.EstadoId
            && item.EstadoDestinoId == estado.Id
            && item.PermisoCodigo == permiso
            && !item.RequiereComentario
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token))
            throw new ConflictoProduccionException("La transición solicitada no está permitida o configurada.");
        orden.EstadoId = estado.Id;
    }

    protected async Task<DocumentoProduccion> DocumentoAsync(
        string serie, ContextoConfiguracion seguridad, CancellationToken token)
    {
        var nombres = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["OP"] = "Orden de producción",
            ["PC"] = "Consumo de producción",
            ["PT"] = "Entrada de producción"
        };
        if (!nombres.TryGetValue(serie, out var nombre))
            throw new ConflictoProduccionException("La serie de producción solicitada no está soportada.");

        var documento = await Contexto.SeriesDocumentos.AsNoTracking().SingleOrDefaultAsync(item =>
            item.EmpresaId == seguridad.EmpresaId && item.SucursalId == null
            && item.Serie == serie && item.TipoDocumento.ModuloId == "PRODUCCION"
            && item.TipoDocumento.Nombre == nombre
            && item.TipoDocumento.ClaseTipo.Nombre == "DOCUMENTOS_PRODUCCION"
            && item.TipoDocumento.Estado.Activo && Operativos.Contains(item.TipoDocumento.Estado.Nombre)
            && item.TipoDocumento.ClaseTipo.Estado.Activo && Operativos.Contains(item.TipoDocumento.ClaseTipo.Estado.Nombre)
            && item.Estado.Activo && Operativos.Contains(item.Estado.Nombre), token)
            ?? throw new ConflictoProduccionException($"Falta la serie {serie} preparada para la empresa.");

        var correlativo = Contexto.CorrelativosDocumentos.Local.FirstOrDefault(item => item.SerieDocumentoId == documento.Id)
            ?? await Contexto.CorrelativosDocumentos.SingleOrDefaultAsync(item => item.SerieDocumentoId == documento.Id, token)
            ?? throw new ConflictoProduccionException("La serie no tiene correlativo configurado.");
        if (correlativo.UltimoNumero == long.MaxValue)
            throw new ConflictoProduccionException("El correlativo se agotó.");
        var numero = correlativo.UltimoNumero + 1;
        var digitos = numero.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var texto = (documento.Prefijo ?? documento.Serie + "-") + digitos.PadLeft(documento.Longitud, '0');
        if (digitos.Length > documento.Longitud || texto.Length > 30)
            throw new ConflictoProduccionException("El correlativo excede la longitud documental configurada.");
        correlativo.UltimoNumero = numero;
        correlativo.FechaModificacion = Ahora;
        return new(documento.Serie, numero, texto);
    }

    protected void Auditar(
        ContextoConfiguracion seguridad, string entidad, object id,
        string accion, string comentario, string modulo)
    {
        static string? Truncar(string? valor, int longitud)
        {
            var texto = string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
            return texto is null ? null : texto[..Math.Min(texto.Length, longitud)];
        }
        Contexto.Bitacoras.Add(new Bitacoras
        {
            EmpresaId = seguridad.EmpresaId,
            SucursalId = seguridad.SucursalId,
            UsuarioId = seguridad.UsuarioId,
            ModuloId = modulo,
            Entidad = entidad,
            EntidadId = Convert.ToString(id, System.Globalization.CultureInfo.InvariantCulture),
            Accion = accion,
            Fecha = Ahora,
            DireccionIp = Truncar(seguridad.DireccionIp, 45),
            Equipo = Truncar(seguridad.Equipo, 200),
            Comentario = Truncar(comentario, 500)
        });
    }

    protected IQueryable<T> Paginar<T>(IQueryable<T> consulta, FiltroPaginadoBase filtro) =>
        consulta.Skip(filtro.CalcularDesplazamiento()).Take(filtro.TamanioPagina);

    protected async Task<T> EscribirAsync<T>(
        ContextoConfiguracion seguridad, Func<Task<T>> operacion, CancellationToken token)
    {
        var clave = seguridad.EmpresaId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await using var bloqueo = await coordinador.AdquirirAsync(clave, token);
        if (Contexto.ChangeTracker.HasChanges())
            throw new ConflictoProduccionException("El contexto contiene cambios pendientes ajenos a la operación.");
        Contexto.ChangeTracker.Clear();
        await using var transaccion = Contexto.Database.IsRelational()
            ? await Contexto.Database.BeginTransactionAsync(IsolationLevel.Serializable, token)
            : null;
        try
        {
            if (Contexto.Database.IsSqlServer())
                await Contexto.Database.ExecuteSqlInterpolatedAsync($"""
                    DECLARE @resultado int;
                    EXEC @resultado = sys.sp_getapplock @Resource = {"Heladeria.Inventario." + clave},
                        @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 10000;
                    IF @resultado < 0 THROW 51002, 'Reserva de producción no disponible.', 1;
                    """, token);
            await ValidarSucursalAsync(seguridad, token);
            var resultado = await operacion();
            await Contexto.SaveChangesAsync(token);
            if (transaccion is not null) await transaccion.CommitAsync(token);
            return resultado;
        }
        catch (Exception error)
        {
            if (transaccion is not null) await transaccion.RollbackAsync(CancellationToken.None);
            Contexto.ChangeTracker.Clear();
            if (error is SqlException { Number: 51060 })
                throw new ConflictoProduccionException(
                    "El esquema de Fase 6 no está aplicado. Ejecute primero el script SQL controlado.");
            if (error is DbUpdateConcurrencyException
                || error is SqlException { Number: 51002 or 1205 or 1222 or 2601 or 2627 }
                || error is DbUpdateException
                    { InnerException: SqlException { Number: 2601 or 2627 } })
                throw new ConflictoProduccionException(
                    "La operación entró en conflicto con otra escritura. Recargue y reintente.");
            throw;
        }
    }

    protected sealed record ProductoProduccion(
        long Id, string Nombre, int UnidadBaseId, string UnidadBase,
        string AbreviaturaBase, string TipoSeguimiento,
        string MetodoValuacion, bool ControlaVencimiento);

    protected sealed record DocumentoProduccion(string Serie, long Numero, string Texto);

    protected sealed record ConversionProduccion(
        int UnidadEntradaId, decimal Factor, decimal CantidadBase);
}
