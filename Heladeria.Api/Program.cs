using Heladeria.Api.Configuracion;
using Heladeria.Api.ManejoErrores;
using Heladeria.Api.Seguridad.Contrasenas;
using Heladeria.Api.Salud;
using Heladeria.Infraestructura;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(opciones =>
    opciones.JsonSerializerOptions.Converters.Add(new ConvertidorFechaApi()));
builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(opciones =>
    opciones.InvalidModelStateResponseFactory = contexto => new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new
    {
        exito = false, mensaje = "Revise los campos obligatorios, formatos y límites de la solicitud.",
        codigo = "VALIDACION_SOLICITUD", traceId = contexto.HttpContext.TraceIdentifier,
        errors = contexto.ModelState.Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(x => x.Key, _ => new[] { "Valor inválido o fuera del límite permitido." })
    }));
// El middleware del framework también registra la excepción antes de llamar al handler. Evitar duplicados crudos.
builder.Logging.AddFilter("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware", LogLevel.None);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorGlobalExcepciones>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Heladeria API",
        Version = "v1",
        Description = "API empresarial para la gestión de Heladeria."
    });
    opciones.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Ingrese únicamente el access token JWT.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    opciones.OperationFilter<FiltroSeguridadSwagger>();
});

builder.Services.AgregarSeguridad(builder.Configuration);
builder.Services.AgregarLimitesSolicitudes(builder.Configuration);
builder.Services.AgregarInfraestructura(
    builder.Configuration,
    habilitarReintentosSql: false);
builder.Services.AgregarCorsSeguro(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();
app.UseEncabezadosSeguridad();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors(ConfiguracionCors.NombrePolitica);
app.UseMiddleware<OrigenAutenticacionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<CambioContrasenaObligatorioMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/salud", new HealthCheckOptions
{
    ResponseWriter = RespuestaSalud.EscribirAsync
}).AllowAnonymous().WithMetadata(new PermitirContrasenaTemporalAttribute());

app.Run();

public partial class Program;
