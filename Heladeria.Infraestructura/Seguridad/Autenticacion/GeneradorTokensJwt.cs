using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Heladeria.Aplicacion.Seguridad.Autorizacion;
using Heladeria.Aplicacion.Seguridad.Configuracion;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Heladeria.Infraestructura.Seguridad.Autenticacion;

public interface IGeneradorTokensJwt
{
    TokenAccesoGenerado CrearAccessToken(
        Usuarios usuario,
        Sucursales sucursal,
        long sesionId,
        bool requiereCambioContrasena);

    string CrearRefreshToken(long sesionId, long sucursalId);

    bool IntentarLeerRefreshToken(
        string token,
        out long sesionId,
        out long sucursalId);

    string CalcularHash(string token);

    bool HashCoincide(string token, string? hashEsperado);
}

public sealed record TokenAccesoGenerado(string Valor, DateTimeOffset ExpiraUtc);

public sealed class GeneradorTokensJwt(
    IOptions<OpcionesSeguridad> opciones,
    TimeProvider timeProvider) : IGeneradorTokensJwt
{
    private readonly OpcionesSeguridad _opciones = opciones.Value;

    public TokenAccesoGenerado CrearAccessToken(
        Usuarios usuario,
        Sucursales sucursal,
        long sesionId,
        bool requiereCambioContrasena)
    {
        var ahora = timeProvider.GetUtcNow();
        var expiracion = ahora.AddMinutes(_opciones.Jwt.MinutosAccessToken);
        var clave = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_opciones.Jwt.ClaveFirma));

        var claims = new[]
        {
            new Claim(ClaimsSeguridad.Sujeto, usuario.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimsSeguridad.NombreUsuario, usuario.UserName),
            new Claim(ClaimsSeguridad.RolId, usuario.RoleId),
            new Claim(ClaimsSeguridad.EmpresaId, sucursal.EmpresaId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimsSeguridad.SucursalId, sucursal.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimsSeguridad.SesionId, sesionId.ToString(CultureInfo.InvariantCulture)),
            new Claim(
                ClaimsSeguridad.RequiereCambioContrasena,
                requiereCambioContrasena ? "true" : "false"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _opciones.Jwt.Emisor,
            audience: _opciones.Jwt.Audiencia,
            claims: claims,
            notBefore: ahora.UtcDateTime,
            expires: expiracion.UtcDateTime,
            signingCredentials: new SigningCredentials(
                clave,
                SecurityAlgorithms.HmacSha256));

        return new TokenAccesoGenerado(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiracion);
    }

    public string CrearRefreshToken(long sesionId, long sucursalId)
    {
        var secreto = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));
        return FormattableString.Invariant($"{sesionId}.{sucursalId}.{secreto}");
    }

    public bool IntentarLeerRefreshToken(
        string token,
        out long sesionId,
        out long sucursalId)
    {
        sesionId = 0;
        sucursalId = 0;

        if (string.IsNullOrWhiteSpace(token) || token.Length > 200)
        {
            return false;
        }

        var partes = token.Split('.', 3, StringSplitOptions.None);
        return partes.Length == 3
            && partes[2].Length >= 80
            && long.TryParse(partes[0], NumberStyles.None, CultureInfo.InvariantCulture, out sesionId)
            && long.TryParse(partes[1], NumberStyles.None, CultureInfo.InvariantCulture, out sucursalId)
            && sesionId > 0
            && sucursalId > 0;
    }

    public string CalcularHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public bool HashCoincide(string token, string? hashEsperado)
    {
        if (string.IsNullOrWhiteSpace(hashEsperado))
        {
            return false;
        }

        try
        {
            var calculado = Convert.FromHexString(CalcularHash(token));
            var esperado = Convert.FromHexString(hashEsperado);
            return CryptographicOperations.FixedTimeEquals(calculado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
