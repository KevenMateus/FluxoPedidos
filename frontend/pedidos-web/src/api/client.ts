import axios from 'axios'
import type {
  AuthResult,
  CreateOrder,
  CustomerLookup,
  DashboardSummary,
  Order,
  OrderFilters,
  OrderListItem,
  OrderStatus,
  PagedResult,
  ProductLookup,
  RevenueReport,
  User,
} from '../types'

const baseURL = import.meta.env.VITE_API_URL ?? 'http://localhost:5005/api'
export const NOTIFICATIONS_URL = import.meta.env.VITE_NOTIFICATIONS_URL ?? 'http://localhost:3001'

export const TOKEN_KEY = 'pedidos_token'

const api = axios.create({ baseURL, timeout: 30000 })

api.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY)
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (r) => r,
  (error) => {
    if (error?.response?.status === 401 && !error.config?.url?.includes('/auth/login')) {
      localStorage.removeItem(TOKEN_KEY)
      window.dispatchEvent(new Event('auth:logout'))
    }
    return Promise.reject(error)
  },
)

export const authApi = {
  login: (email: string, password: string) =>
    api.post<AuthResult>('/auth/login', { email, password }).then((r) => r.data),
  me: () => api.get<User>('/auth/me').then((r) => r.data),
}

export const ordersApi = {
  list: (page: number, pageSize: number, filters: OrderFilters) =>
    api
      .get<PagedResult<OrderListItem>>('/orders', {
        params: { page, pageSize, ...cleanFilters(filters) },
      })
      .then((r) => r.data),

  getById: (id: string) => api.get<Order>(`/orders/${id}`).then((r) => r.data),

  create: (payload: CreateOrder) => api.post<Order>('/orders', payload).then((r) => r.data),

  changeStatus: (id: string, status: OrderStatus, note?: string) =>
    api.patch<Order>(`/orders/${id}/status`, { status, note }).then((r) => r.data),
}

export const catalogApi = {
  customers: () =>
    api.get<CustomerLookup[]>('/catalog/customers', { params: { take: 200 } }).then((r) => r.data),
  products: () =>
    api.get<ProductLookup[]>('/catalog/products', { params: { take: 200 } }).then((r) => r.data),
}

export const reportsApi = {
  revenue: (from: string, to: string) =>
    api.get<RevenueReport>('/reports/revenue', { params: { from, to } }).then((r) => r.data),
  dashboard: () => api.get<DashboardSummary>('/reports/dashboard').then((r) => r.data),
  downloadCsv: async (from: string, to: string) => {
    const resp = await api.get(`/reports/revenue/csv`, {
      params: { from, to },
      responseType: 'blob',
    })
    return resp.data as Blob
  },
}

function cleanFilters(filters: OrderFilters): Record<string, string> {
  const out: Record<string, string> = {}
  if (filters.status) out.status = filters.status
  if (filters.search) out.search = filters.search
  if (filters.from) out.from = filters.from
  if (filters.to) out.to = filters.to
  return out
}
