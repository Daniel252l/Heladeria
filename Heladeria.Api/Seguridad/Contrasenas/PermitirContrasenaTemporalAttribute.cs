namespace Heladeria.Api.Seguridad.Contrasenas;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PermitirContrasenaTemporalAttribute : Attribute
{
}
