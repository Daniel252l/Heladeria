using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Ventas;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Aplicacion.Compras;
using Heladeria.Aplicacion.Productos;
using Heladeria.Aplicacion.Produccion;
using Heladeria.Aplicacion.Seguridad.Excepciones;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Api.ManejoErrores;

public sealed class ManejadorGlobalExcepciones(
    ILogger<ManejadorGlobalExcepciones> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext contexto,
        Exception excepcion,
        CancellationToken cancellationToken)
    {
        var (estado, mensaje, codigo) = excepcion switch
        {
            Heladeria.Aplicacion.Reportes.ExcepcionReporte error => (error.EstadoHttp, error.Message, error.Codigo),
            Heladeria.Aplicacion.Clientes.ExcepcionClientes error => (error.EstadoHttp, error.Message, error.Codigo),
            ExcepcionCaja error => (error.EstadoHttp, error.Message, error.Codigo),
            VentaNoEncontradaException error => (StatusCodes.Status404NotFound, error.Message, error.Codigo),
            ConflictoVentasException error => (StatusCodes.Status409Conflict, error.Message, error.Codigo),
            StockVentaInsuficienteException error => (StatusCodes.Status409Conflict, error.Message, error.Codigo),
            ValidacionVentasException error => (StatusCodes.Status400BadRequest, error.Message, error.Codigo),
            RecursoProduccionNoEncontradoException error => (StatusCodes.Status404NotFound, error.Message, error.Codigo),
            ExistenciaProduccionInsuficienteException error => (StatusCodes.Status409Conflict, error.Message, error.Codigo),
            ConflictoProduccionException error => (StatusCodes.Status409Conflict, error.Message, error.Codigo),
            ValidacionProduccionException error => (StatusCodes.Status400BadRequest, error.Message, error.Codigo),
            RecursoComprasNoEncontradoException error => (StatusCodes.Status404NotFound, error.Message, error.Codigo),
            ConflictoComprasException error => (StatusCodes.Status409Conflict, error.Message, error.Codigo),
            ValidacionComprasException error => (StatusCodes.Status400BadRequest, error.Message, error.Codigo),
            CredencialesInvalidasException => (
                StatusCodes.Status401Unauthorized,
                excepcion.Message,
                ((ExcepcionSeguridad)excepcion).Codigo),
            TokenRenovacionInvalidoException => (
                StatusCodes.Status401Unauthorized,
                excepcion.Message,
                ((ExcepcionSeguridad)excepcion).Codigo),
            SesionActivaException => (
                StatusCodes.Status409Conflict,
                excepcion.Message,
                ((ExcepcionSeguridad)excepcion).Codigo),
            AccesoDenegadoException => (
                StatusCodes.Status403Forbidden,
                excepcion.Message,
                ((ExcepcionSeguridad)excepcion).Codigo),
            ValidacionSeguridadException => (
                StatusCodes.Status400BadRequest,
                excepcion.Message,
                ((ExcepcionSeguridad)excepcion).Codigo),
            RecursoConfiguracionNoEncontradoException => (
                StatusCodes.Status404NotFound,
                excepcion.Message,
                ((ExcepcionConfiguracion)excepcion).Codigo),
            ConflictoConfiguracionException => (
                StatusCodes.Status409Conflict,
                excepcion.Message,
                ((ExcepcionConfiguracion)excepcion).Codigo),
            ValidacionConfiguracionException => (
                StatusCodes.Status400BadRequest,
                excepcion.Message,
                ((ExcepcionConfiguracion)excepcion).Codigo),
            RecursoFase4NoEncontradoException => (
                StatusCodes.Status404NotFound,
                excepcion.Message,
                ((ExcepcionFase4)excepcion).Codigo),
            ConflictoFase4Exception => (
                StatusCodes.Status409Conflict,
                excepcion.Message,
                ((ExcepcionFase4)excepcion).Codigo),
            ExistenciaInsuficienteException => (
                StatusCodes.Status409Conflict,
                excepcion.Message,
                ((ExcepcionFase4)excepcion).Codigo),
            ConcurrenciaInventarioException => (
                StatusCodes.Status409Conflict,
                excepcion.Message,
                ((ExcepcionFase4)excepcion).Codigo),
            ValidacionFase4Exception => (
                StatusCodes.Status400BadRequest,
                excepcion.Message,
                ((ExcepcionFase4)excepcion).Codigo),
            DbUpdateException => (
                StatusCodes.Status409Conflict,
                "La operación entra en conflicto con los datos actuales.",
                "CONFLICTO_PERSISTENCIA"),
            _ => (
                StatusCodes.Status500InternalServerError,
                "No fue posible completar la operación.",
                "ERROR_INTERNO")
        };

        if (estado == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                "Error no controlado. Tipo: {Tipo}, TraceId: {TraceId}",
                excepcion.GetType().Name, contexto.TraceIdentifier);
        }
        else
        {
            logger.LogInformation(
                "Solicitud rechazada. Codigo: {Codigo}, TraceId: {TraceId}",
                codigo,
                contexto.TraceIdentifier);
        }

        contexto.Response.StatusCode = estado;

        await contexto.Response.WriteAsJsonAsync(
            new RespuestaError(
                false,
                mensaje,
                codigo,
                contexto.TraceIdentifier),
            cancellationToken);

        return true;
    }
}

public sealed record RespuestaError(
    bool Exito,
    string Mensaje,
    string Codigo,
    string TraceId);
