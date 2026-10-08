namespace Heladeria.Aplicacion.Seguridad.Configuracion;

public sealed class OpcionesSeguridad
{
    public const string Seccion = "Seguridad";

    public OpcionesJwt Jwt { get; init; } = new();

    public OpcionesContrasenas Contrasenas { get; init; } = new();

    public OpcionesBloqueo Bloqueo { get; init; } = new();

    public OpcionesCookieRefresh CookieRefresh { get; init; } = new();

    public OpcionesRateLimiting RateLimiting { get; init; } = new();

    public string[] EstadosActivos { get; init; } = ["Activo"];

    public string ZonaHoraria { get; init; } = "America/Guatemala";

    public Dictionary<int, string> ZonasHorariasEmpresas { get; init; } = [];
    public Dictionary<long, string> ZonasHorariasSucursales { get; init; } = [];
}

public sealed class OpcionesJwt
{
    public string Emisor { get; init; } = "Heladeria.Api";

    public string Audiencia { get; init; } = "Heladeria.Web";

    public string ClaveFirma { get; init; } = string.Empty;

    public int MinutosAccessToken { get; init; } = 10;

    public int DiasRefreshToken { get; init; } = 7;
}

public sealed class OpcionesContrasenas
{
    public int LongitudMinima { get; init; } = 10;

    public bool RequiereMayuscula { get; init; } = true;

    public bool RequiereMinuscula { get; init; } = true;

    public bool RequiereNumero { get; init; } = true;

    public bool RequiereCaracterEspecial { get; init; } = true;

    public bool AplicarVencimientoPorRolEnMeses { get; init; } = true;
}

public sealed class OpcionesBloqueo
{
    public int IntentosMaximos { get; init; } = 5;

    public int MinutosBloqueo { get; init; } = 15;
}

public sealed class OpcionesCookieRefresh
{
    public string Nombre { get; init; } = "heladeria.refresh";

    public string SameSite { get; init; } = "Strict";
}

public sealed class OpcionesRateLimiting
{
    public int IntentosLogin { get; init; } = 10;

    public int Renovaciones { get; init; } = 20;

    public int CambiosContrasena { get; init; } = 5;

    public int VentanaSegundos { get; init; } = 60;
}
