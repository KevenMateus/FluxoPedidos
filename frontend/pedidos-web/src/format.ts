import type { OrderStatus } from './types'

export const brl = (value: number) =>
  value.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' })

export const dateTime = (iso: string) =>
  new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })

export const dateOnly = (iso: string) => new Date(iso).toLocaleDateString('pt-BR')

/** Classe CSS do badge por status (cores semânticas da paleta). */
export const statusClass = (status: OrderStatus) => `badge badge-${status.toLowerCase()}`
