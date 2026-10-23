using System.Text.RegularExpressions;
using Heladeria.Aplicacion.Caja;
using Heladeria.Aplicacion.Configuracion;
using Heladeria.Infraestructura.Persistencia.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Heladeria.Infraestructura.Caja;

public sealed partial class ServicioCaja
{
    public Task<CajaDto> CrearCajaAsync(CrearCajaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud);
            var codigo = solicitud.Codigo.Trim().ToUpperInvariant();
            if (!Regex.IsMatch(codigo, "^[A-Z0-9_-]{1,20}$") || string.IsNullOrWhiteSpace(solicitud.Nombre))
                throw new ValidacionCajaException("Indique código alfanumérico y nombre de caja.");
            if (await Contexto.Cajas.AnyAsync(x => x.SucursalId == s.SucursalId && x.Codigo == codigo, t))
                throw new ExcepcionCaja("El código de caja ya existe en esta sucursal.");
            var caja = new Cajas { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, Codigo = codigo,
                Nombre = solicitud.Nombre.Trim(), EstadoId = await ActivoAsync(t) };
            Contexto.Cajas.Add(caja); await Contexto.SaveChangesAsync(t);
            Auditar(s, caja.Id, "CAJA_CREADA", "Caja registrada explícitamente, sin apertura automática.", "Cajas");
            return new CajaDto(caja.Id, caja.EmpresaId, caja.SucursalId, caja.Codigo, caja.Nombre,
                caja.EstadoId, await Contexto.Estados.Where(x => x.Id == caja.EstadoId).Select(x => x.Nombre).SingleAsync(t), null);
        }, t, "OPERACION_CAJA_RECHAZADA");

    public Task<SesionCajaDto> AbrirAsync(AbrirCajaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud); Importe(solicitud.SaldoInicial, "Saldo inicial", true);
            var caja = await Contexto.Cajas.AsNoTracking().Include(x => x.Estado)
                .SingleOrDefaultAsync(x => x.Id == solicitud.CajaId && x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId, t)
                ?? throw new CajaNoEncontradaException("Caja no encontrada en la sucursal activa.");
            if (!caja.Estado.Activo || !Operativos.Contains(caja.Estado.Nombre)) throw new ExcepcionCaja("La caja no está activa.");
            if (await Contexto.TurnosCaja.AnyAsync(x => x.CajaId == caja.Id && x.FechaCierre == null, t))
                throw new ExcepcionCaja("La caja ya tiene una sesión abierta.", "CAJA_YA_ABIERTA");
            var moneda = await MonedaAsync(s, t);
            if (moneda.Id != solicitud.MonedaId) throw new ValidacionCajaException("La moneda debe ser la moneda base operativa de la empresa.");
            var efectivo = await EfectivoAsync(s, t);
            var estado = await EstadoAsync("ABIERTA", t);
            var turno = new TurnosCaja { CajaId = caja.Id, UsuarioId = s.UsuarioId, FechaApertura = Ahora,
                FechaModificacion = Ahora, MontoInicial = solicitud.SaldoInicial, MonedaId = moneda.Id,
                FlujoEstadoId = estado.FlujoEstadoId, EstadoId = estado.Id,
                NumeroApertura = await DocumentoAsync("CA", s, t) };
            Contexto.TurnosCaja.Add(turno); await Contexto.SaveChangesAsync(t);
            // El CHECK histórico de movimientos exige > 0. Una apertura cero queda explícita en turno y bitácora.
            if (turno.MontoInicial > 0)
                await AgregarMovimientoAsync(turno, "APERTURA_CAJA", turno.MontoInicial, efectivo.Id, true,
                    "Saldo inicial; no es ingreso operativo ni venta.", turno.NumeroApertura, null, s, t);
            Auditar(s, turno.Id, "CAJA_ABIERTA", FormattableString.Invariant($"Apertura {turno.NumeroApertura}; saldo inicial real {turno.MontoInicial:0.00}; moneda {moneda.CodigoISO}."));
            await Contexto.SaveChangesAsync(t);
            return await LeerSesionAsync(turno.Id, s, t);
        }, t, "OPERACION_CAJA_RECHAZADA");

    internal async Task<TurnosCaja> AbiertaAsync(long id, string version, ContextoConfiguracion s, CancellationToken t)
    {
        var turno = await CargarAsync(id, s, t);
        if (!EsFase8(turno)) throw new ExcepcionCaja("La sesión histórica requiere conciliación explícita antes de operar.");
        if (turno.FechaCierre is not null || turno.EstadosFlujo.Codigo != "ABIERTA")
            throw new ExcepcionCaja("La sesión de caja no está abierta.", "CAJA_NO_ABIERTA");
        if (!turno.Caja.Estado.Activo || !Operativos.Contains(turno.Caja.Estado.Nombre)) throw new ExcepcionCaja("La caja no está activa.");
        if ((await EstadoAsync("ABIERTA", t)).Id != turno.EstadoId) throw new ExcepcionCaja("Estado de sesión incompatible.");
        if ((await MonedaAsync(s, t)).Id != turno.MonedaId) throw new ExcepcionCaja("La moneda de la sesión ya no está operativa.");
        Version(turno.VersionFila, version);
        return turno;
    }
    internal void Tocar(TurnosCaja turno)
    {
        turno.FechaModificacion = Ahora;
        Contexto.Entry(turno).Property(x => x.FechaModificacion).IsModified = true;
    }
    internal async Task AgregarMovimientoAsync(TurnosCaja turno, string concepto, decimal monto, int forma, bool efectivo,
        string descripcion, string? referencia, PagoDetalles? detalle, ContextoConfiguracion s, CancellationToken t)
    {
        Contexto.MovimientosCaja.Add(new MovimientosCaja { TurnoCajaId = turno.Id,
            TipoMovimientoId = await TipoAsync("MOVIMIENTOS_CAJA", concepto, t), CodigoConcepto = concepto,
            Fecha = Ahora, Monto = monto, FormaPagoId = forma, EsEfectivo = efectivo,
            Referencia = referencia, Descripcion = descripcion, UsuarioId = s.UsuarioId, PagoDetalle = detalle,
            NumeroDocumento = await DocumentoAsync("MC", s, t) });
    }
    public Task<SesionCajaDto> MovimientoAsync(long id, MovimientoCajaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud); Importe(solicitud.Monto, "Monto");
            if (solicitud.Motivo.Trim().Length < 3) throw new ValidacionCajaException("Indique un motivo de al menos tres caracteres.");
            var turno = await AbiertaAsync(id, solicitud.VersionFila, s, t);
            var actual = await LeerSesionAsync(id, s, t);
            if (solicitud.Tipo == "EGRESO" && solicitud.Monto > actual.EfectivoEsperado)
                throw new ExcepcionCaja("El egreso excede el efectivo esperado; no se admite saldo negativo.");
            var concepto = solicitud.Tipo == "INGRESO" ? "INGRESO_CAJA" : "EGRESO_CAJA";
            await AgregarMovimientoAsync(turno, concepto, solicitud.Monto, (await EfectivoAsync(s, t)).Id, true,
                solicitud.Motivo.Trim() + (Opcional(solicitud.Observacion) is { } obs ? " — " + obs : ""), null, null, s, t);
            Tocar(turno); Auditar(s, id, "MOVIMIENTO_CAJA_CREADO", $"Movimiento manual {concepto}; no representa una venta.");
            await Contexto.SaveChangesAsync(t);
            return await LeerSesionAsync(id, s, t);
        }, t, "OPERACION_CAJA_RECHAZADA");

    public Task<ResultadoCobroCajaDto> CobrarAsync(long id, CobrarVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, () => CobrarEnTransaccionAsync(id, solicitud, s, t), t, "COBRO_RECHAZADO");

    // Solo para operaciones que ya poseen la reserva empresarial y la transacción del mismo DbContext.
    // No vuelve a bloquear, limpiar el tracker ni confirmar una transacción independiente.
    internal async Task<ResultadoCobroCajaDto> CobrarEnTransaccionAsync(long id, CobrarVentaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t)
    {
        if (Contexto.Database.IsRelational() && Contexto.Database.CurrentTransaction is null)
            throw new InvalidOperationException("El cobro integrado requiere una transacción activa.");
        Validar(solicitud);
        if (solicitud.ClaveOperacion == Guid.Empty) throw new ValidacionCajaException("ClaveOperacion debe ser una UUID no vacía.");
        foreach (var pago in solicitud.Pagos)
        {
            if (pago is null) throw new ValidacionCajaException("Cada forma de cobro es obligatoria.");
            Validar(pago); Importe(pago.MontoAplicado, "Monto aplicado"); Importe(pago.MontoRecibido, "Monto recibido");
            ReferenciaSegura(pago.Referencia);
        }
        if (solicitud.Pagos.Select(x => x.FormaPagoId).Distinct().Count() != solicitud.Pagos.Length)
            throw new ValidacionCajaException("Agrupe los importes de cada forma de pago en una sola línea.");
        // Alcance antes de idempotencia; el reintento de un cobro confirmado es válido incluso tras cerrar sesión.
        await CargarAsync(id, s, t);
        var previo = await Contexto.Pagos.AsNoTracking().Include(x => x.PagoDetalles)
            .SingleOrDefaultAsync(x => x.EmpresaId == s.EmpresaId && x.ClaveOperacion == solicitud.ClaveOperacion, t);
        if (previo is not null)
        {
            if (previo.SucursalId != s.SucursalId || previo.TurnoCajaId != id || previo.VentaId != solicitud.VentaId
                || previo.PagoDetalles.Count != solicitud.Pagos.Length
                || previo.PagoDetalles.Any(d => !solicitud.Pagos.Any(p => p.FormaPagoId == d.FormaPagoId
                    && p.MontoAplicado == d.Monto && p.MontoRecibido == d.MontoRecibido && Opcional(p.Referencia) == d.Referencia)))
                throw new ExcepcionCaja("La UUID ya corresponde a otra solicitud de cobro.", "COBRO_INVALIDO");
            var existente = await LeerSesionAsync(id, s, t);
            return new ResultadoCobroCajaDto(existente.Cobros.Single(x => x.Id == previo.Id), existente, true);
        }
        var turno = await AbiertaAsync(id, solicitud.VersionFila, s, t);
        await LeerSesionAsync(id, s, t); // Verifica integridad previa del libro de caja.
        var venta = await Contexto.Ventas.AsNoTracking().Include(x => x.EstadosFlujo).ThenInclude(x => x.FlujoEstado)
            .SingleOrDefaultAsync(x => x.Id == solicitud.VentaId && x.EmpresaId == s.EmpresaId && x.SucursalId == s.SucursalId, t)
            ?? throw new CajaNoEncontradaException("Venta no encontrada en la sucursal activa.");
        if (await Contexto.Pagos.AnyAsync(x => x.VentaId == venta.Id, t))
            throw new ExcepcionCaja("La venta ya está cobrada.", "VENTA_YA_COBRADA");
        if (venta.EstadosFlujo.FlujoEstado.Codigo != "VENTA_POS" || venta.EstadosFlujo.Codigo != "COMPLETADA"
            || venta.FechaCompletada is null || venta.BodegaId is null || venta.ClaveOperacion is null || venta.EsCredito
            || venta.FechaAnulacion is not null || venta.TurnoCajaId is not null || venta.PedidoVentaId is not null
            || await Contexto.DevolucionesVentas.AnyAsync(x => x.VentaId == venta.Id, t)
            || await Contexto.NotasCredito.AnyAsync(x => x.VentaId == venta.Id, t)
            || await Contexto.NotasDebito.AnyAsync(x => x.VentaId == venta.Id, t)
            || await Contexto.CuentasPorCobrar.AnyAsync(x => x.VentaId == venta.Id, t))
            throw new ExcepcionCaja("Solo puede cobrarse una venta POS completada, conciliada y sin crédito.", "COBRO_INVALIDO");
        Version(venta.VersionFila, solicitud.VersionVenta);
        if (venta.MonedaId != turno.MonedaId || venta.TipoCambio != 1) throw new ExcepcionCaja("La moneda de venta no coincide con la sesión.", "COBRO_INVALIDO");
        Importe(venta.TotalVenta, "Total a cobrar");
        if (solicitud.Pagos.Sum(x => x.MontoAplicado) != venta.TotalVenta)
            throw new ExcepcionCaja("La suma aplicada debe ser exactamente el total de la venta.", "COBRO_INVALIDO");
        var formas = await FormasAsync(s, t);
        var cobro = new Pagos { EmpresaId = s.EmpresaId, SucursalId = s.SucursalId, ClienteId = venta.ClienteId,
            Fecha = Ahora, MonedaId = venta.MonedaId, TipoCambio = 1, MontoTotal = venta.TotalVenta,
            TipoPagoOperacionId = await TipoAsync("OPERACIONES_PAGO_CAJA", "COBRO_VENTA_POS", t),
            TurnoCajaId = id, UsuarioId = s.UsuarioId, EstadoId = await ActivoAsync(t), VentaId = venta.Id,
            ClaveOperacion = solicitud.ClaveOperacion };
        foreach (var p in solicitud.Pagos)
        {
            var forma = formas.SingleOrDefault(x => x.Id == p.FormaPagoId)
                ?? throw new ExcepcionCaja("Forma de pago no operativa, de otra empresa, de crédito o incompatible.", "COBRO_INVALIDO");
            var efectivo = forma.Codigo == "EFECTIVO";
            if (p.MontoRecibido < p.MontoAplicado || !efectivo && p.MontoRecibido != p.MontoAplicado
                || forma.RequiereReferencia && Opcional(p.Referencia) is null)
                throw new ExcepcionCaja("Revise monto recibido, vuelto y referencia de la forma de pago.", "COBRO_INVALIDO");
            var detalle = new PagoDetalles { FormaPagoId = forma.Id, Monto = p.MontoAplicado,
                MontoRecibido = p.MontoRecibido, EsEfectivo = efectivo, Referencia = Opcional(p.Referencia) };
            cobro.PagoDetalles.Add(detalle);
        }
        Contexto.Pagos.Add(cobro); await Contexto.SaveChangesAsync(t);
        foreach (var d in cobro.PagoDetalles)
            await AgregarMovimientoAsync(turno, "COBRO_VENTA", d.Monto, d.FormaPagoId, d.EsEfectivo == true,
                "Cobro total de venta POS; importe retenido, sin incluir vuelto.", venta.NumeroFactura, d, s, t);
        Tocar(turno); Auditar(s, id, "VENTA_COBRADA", $"Venta {venta.Id}; pago {cobro.Id}; clave {solicitud.ClaveOperacion}.");
        await Contexto.SaveChangesAsync(t);
        var resultado = await LeerSesionAsync(id, s, t);
        return new ResultadoCobroCajaDto(resultado.Cobros.Single(x => x.Id == cobro.Id), resultado, false);
    }

    internal static void ReferenciaSegura(string? referencia)
    {
        if (Opcional(referencia) is not { } valor) return;
        // Solo identificadores cortos de autorización. No texto libre ni PAN con separadores ni campos CVV/PAN.
        if (!Regex.IsMatch(valor, "^[A-Za-z0-9._-]{1,30}$") || valor.Count(char.IsDigit) >= 13
            || Regex.IsMatch(valor, "(?i)(cvv|cvc|pan|tarjeta)"))
            throw new ValidacionCajaException("La referencia debe ser un identificador seguro; nunca PAN, CVV ni datos de tarjeta.");
    }

    public Task<SesionCajaDto> ArqueoAsync(long id, ArqueoCajaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud); Importe(solicitud.EfectivoContado, "Efectivo contado", true);
            var turno = await AbiertaAsync(id, solicitud.VersionFila, s, t);
            var actual = await LeerSesionAsync(id, s, t);
            Importe(actual.EfectivoEsperado!.Value, "Efectivo esperado", true);
            Tocar(turno); await Contexto.SaveChangesAsync(t);
            await Contexto.Entry(turno).ReloadAsync(t);
            Contexto.ArqueosCaja.Add(new ArqueosCaja { TurnoCajaId = id, Fecha = Ahora,
                MontoEsperado = actual.EfectivoEsperado.Value, MontoContado = solicitud.EfectivoContado,
                UsuarioId = s.UsuarioId, Observaciones = Opcional(solicitud.Observaciones), VersionTurno = turno.VersionFila.ToArray() });
            Auditar(s, id, "ARQUEO_REGISTRADO", $"Contado {solicitud.EfectivoContado}; esperado {actual.EfectivoEsperado}; diferencia {solicitud.EfectivoContado - actual.EfectivoEsperado}.");
            await Contexto.SaveChangesAsync(t);
            return await LeerSesionAsync(id, s, t);
        }, t, "OPERACION_CAJA_RECHAZADA");

    public Task<SesionCajaDto> CerrarAsync(long id, CerrarCajaSolicitud solicitud, ContextoConfiguracion s, CancellationToken t) =>
        EscribirAsync(s, async () =>
        {
            Validar(solicitud);
            var turno = await AbiertaAsync(id, solicitud.VersionFila, s, t);
            var actual = await LeerSesionAsync(id, s, t);
            var arqueo = actual.Arqueos.LastOrDefault();
            if (arqueo is null || arqueo.Id != solicitud.ArqueoId || !arqueo.Vigente || arqueo.Esperado != actual.EfectivoEsperado)
                throw new ExcepcionCaja("Registre un arqueo vigente después del último movimiento para cerrar.");
            var estado = await EstadoAsync("CERRADA", t);
            if (!await Contexto.EstadoTransiciones.AsNoTracking().AnyAsync(x => x.FlujoEstadoId == turno.FlujoEstadoId
                && x.EstadoOrigenId == turno.EstadoId && x.EstadoDestinoId == estado.Id && x.PermisoCodigo == "CAJA.CERRAR"
                && x.Estado.Activo && Operativos.Contains(x.Estado.Nombre), t))
                throw new ExcepcionCaja("La transición de cierre no está configurada.");
            turno.TotalIngresos = actual.Movimientos.Where(x => x.Tipo is "INGRESO_CAJA" or "COBRO_VENTA" or "COBRO_CUENTA_POR_COBRAR").Sum(x => x.Monto);
            turno.TotalEgresos = actual.Movimientos.Where(x => x.Tipo is "EGRESO_CAJA" or "PAGO_CUENTA_POR_PAGAR").Sum(x => x.Monto);
            Importe(turno.TotalIngresos.Value, "Ingresos acumulados", true); Importe(turno.TotalEgresos.Value, "Egresos acumulados", true);
            turno.MontoEsperado = arqueo.Esperado; turno.MontoFisico = arqueo.Contado;
            turno.ArqueoCierreId = arqueo.Id; turno.FechaCierre = Ahora; turno.UsuarioCierreId = s.UsuarioId;
            turno.NumeroCierre = await DocumentoAsync("CC", s, t); turno.EstadoId = estado.Id; Tocar(turno);
            Auditar(s, id, "CAJA_CERRADA", $"Cierre {turno.NumeroCierre}; arqueo {arqueo.Id}; diferencia {arqueo.Diferencia}. Sin ajuste automático.");
            await Contexto.SaveChangesAsync(t);
            return await LeerSesionAsync(id, s, t);
        }, t, "OPERACION_CAJA_RECHAZADA");
}
