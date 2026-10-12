using System.ComponentModel.DataAnnotations;
using Heladeria.Aplicacion.Configuracion;

namespace Heladeria.Aplicacion.Clientes;

public sealed class CrearClienteSolicitud : IValidatableObject
{
    [Required, StringLength(20)]
    public string CodigoCliente { get; init; } = string.Empty;

    [Required, StringLength(150)]
    public string NombreCliente { get; init; } = string.Empty;

    [StringLength(20)]
    public string? Nit { get; init; }

    [StringLength(20)]
    public string? Telefono { get; init; }

    [StringLength(100), EmailAddress]
    public string? Correo { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true)]
    public decimal LimiteCredito { get; init; }

    [Range(0, short.MaxValue)]
    public short DiasCredito { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (LimiteCredito != decimal.Round(LimiteCredito, 2))
            yield return new("El límite de crédito admite como máximo dos decimales.", [nameof(LimiteCredito)]);
        if (new[] { CodigoCliente, NombreCliente, Nit, Telefono, Correo }
            .Any(texto => texto?.Any(char.IsControl) == true))
            yield return new("Los datos del cliente no pueden contener caracteres de control.");
    }
}

public sealed record ClienteConsultaDto(int Id, int EmpresaId, string CodigoCliente, string NombreCliente,
    string? Nit, string? Telefono, string? Correo, decimal LimiteCredito, short DiasCredito, int EstadoId, string Estado);

public interface IServicioClientes
{
    Task<RespuestaPaginada<ClienteConsultaDto>> ListarAsync(FiltroConEstadoSolicitud filtro, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ClienteConsultaDto> ObtenerAsync(int id, ContextoConfiguracion seguridad, CancellationToken token);
    Task<ClienteConsultaDto> CrearAsync(CrearClienteSolicitud solicitud, ContextoConfiguracion seguridad, CancellationToken token);
}

public sealed class ExcepcionClientes(string mensaje, string codigo, int estadoHttp) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
    public int EstadoHttp { get; } = estadoHttp;
}
