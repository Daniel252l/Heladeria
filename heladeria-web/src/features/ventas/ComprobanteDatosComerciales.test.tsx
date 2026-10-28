import { screen, within } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { PERMISOS } from '../../auth/permisos'
import { ticketDemo } from '../../test/datosAjusteC'
import { renderSeguridad } from '../../test/renderSeguridad'
import { ComprobanteVenta } from './PaginaComprobanteVenta'

describe('Corrección C.1 · datos comerciales del comprobante', () => {
  it.each([null, undefined, '', ' \t\n ', 'Pendiente', 'Pendiente de configurar', '  pEnDiEnTe De \t Configurar  ', 'PENDIENTE.', 'Sin configurar', 'Por configurar', 'Pendiente de configuración'])('omite completamente líneas opcionales con valor %j', valor => {
    const venta = structuredClone(ticketDemo)
    Object.assign(venta.comprobante!, { razonSocial: valor, direccionSucursal: valor, telefonoSucursal: valor })
    renderSeguridad(<ComprobanteVenta venta={venta} />, [PERMISOS.ventasConsultar])
    const ticket = screen.getByRole('article', { name: 'Comprobante de venta' })
    const encabezado = ticket.querySelector('header')!
    expect(within(encabezado).getByRole('heading', { name: 'Heladería' })).toBeDefined()
    expect(Array.from(encabezado.querySelectorAll('p'), p => p.textContent)).toEqual(['Sucursal principal'])
    expect(ticket.textContent).not.toMatch(/pendiente|sin configurar|por configurar/i)
    expect(ticket.textContent).toContain('TOTALGTQ 50.00')
    expect(ticket.textContent).toContain('VueltoGTQ 5.00')
  })

  it('conserva datos reales, incluso nombres y direcciones que contienen Pendiente', () => {
    const venta = structuredClone(ticketDemo)
    Object.assign(venta.comprobante!, { empresa: 'Heladería La Pendiente', razonSocial: 'Heladería La Pendiente S.A.', direccionSucursal: ' Calle La Pendiente 12 ', telefonoSucursal: ' 2222-0000 ' })
    venta.encabezado.sucursal = 'Sucursal La Pendiente'
    renderSeguridad(<ComprobanteVenta venta={venta} />, [PERMISOS.ventasConsultar])
    const encabezado = screen.getByRole('article').querySelector('header')!
    expect(within(encabezado).getByRole('heading', { name: 'Heladería La Pendiente' })).toBeDefined()
    expect(Array.from(encabezado.querySelectorAll('p'), p => p.textContent)).toEqual(['Heladería La Pendiente S.A.', 'Sucursal La Pendiente', 'Calle La Pendiente 12', '2222-0000'])
  })

  it('omite solo el campo pendiente y evita duplicar razón social igual al nombre', () => {
    const venta = structuredClone(ticketDemo)
    Object.assign(venta.comprobante!, { razonSocial: ' Heladería ', direccionSucursal: 'Pendiente de configurar' })
    renderSeguridad(<ComprobanteVenta venta={venta} />, [PERMISOS.ventasConsultar])
    const encabezado = screen.getByRole('article').querySelector('header')!
    expect(Array.from(encabezado.querySelectorAll('p'), p => p.textContent)).toEqual(['Sucursal principal', '2222-0000'])
  })
})
