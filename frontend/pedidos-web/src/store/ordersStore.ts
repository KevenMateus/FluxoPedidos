import { create } from 'zustand'
import { ordersApi } from '../api/client'
import type { CreateOrder, Order, OrderFilters, OrderListItem, OrderStatus } from '../types'

interface OrdersState {
  items: OrderListItem[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
  filters: OrderFilters
  loading: boolean
  error: string | null

  expandedId: string | null
  expandedOrder: Order | null
  expandedLoading: boolean

  fetch: (page?: number) => Promise<void>
  setPageSize: (pageSize: number) => Promise<void>
  setFilters: (filters: OrderFilters) => Promise<void>
  createOrder: (payload: CreateOrder) => Promise<void>
  toggleExpand: (id: string) => Promise<void>
  refreshExpanded: () => Promise<void>
  changeStatus: (id: string, status: OrderStatus, note?: string) => Promise<void>
}

export const useOrdersStore = create<OrdersState>((set, get) => ({
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
  filters: {},
  loading: false,
  error: null,
  expandedId: null,
  expandedOrder: null,
  expandedLoading: false,

  fetch: async (page = get().page) => {
    set({ loading: true, error: null })
    try {
      const result = await ordersApi.list(page, get().pageSize, get().filters)
      set({
        items: result.items,
        page: result.page,
        totalCount: result.totalCount,
        totalPages: result.totalPages,
        loading: false,
      })
    } catch (e) {
      set({ loading: false, error: extractError(e) })
    }
  },

  setPageSize: async (pageSize) => {
    set({ pageSize, page: 1 })
    await get().fetch(1)
  },

  setFilters: async (filters) => {
    set({ filters, page: 1 })
    await get().fetch(1)
  },

  createOrder: async (payload) => {
    await ordersApi.create(payload)
    await get().fetch(1)
  },

  toggleExpand: async (id) => {
    if (get().expandedId === id) {
      set({ expandedId: null, expandedOrder: null })
      return
    }
    set({ expandedId: id, expandedOrder: null, expandedLoading: true })
    try {
      const order = await ordersApi.getById(id)
      set({ expandedOrder: order, expandedLoading: false })
    } catch (e) {
      set({ expandedLoading: false, error: extractError(e) })
    }
  },

  refreshExpanded: async () => {
    const id = get().expandedId
    if (!id) return
    const order = await ordersApi.getById(id)
    set({ expandedOrder: order })
  },

  changeStatus: async (id, status, note) => {
    const updated = await ordersApi.changeStatus(id, status, note)
    set((s) => ({
      expandedOrder: s.expandedId === id ? updated : s.expandedOrder,
      items: s.items.map((o) =>
        o.id === id ? { ...o, status: updated.status, statusLabel: updated.statusLabel } : o,
      ),
    }))
  },
}))

function extractError(e: unknown): string {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const resp = (e as { response?: { data?: { detail?: string } } }).response
    if (resp?.data?.detail) return resp.data.detail
  }
  return 'Falha ao comunicar com a API.'
}
