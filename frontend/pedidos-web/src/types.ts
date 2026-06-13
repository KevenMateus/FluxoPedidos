
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type OrderStatus = 'Pending' | 'Paid' | 'Shipped' | 'Delivered' | 'Cancelled'
export type PaymentMethod = 'Pix' | 'Boleto' | 'CreditCard' | 'Cash'

export interface OrderListItem {
  id: string
  customerId: string
  customerName: string
  createdAt: string
  status: OrderStatus
  statusLabel: string
  paymentMethod: PaymentMethod
  paymentMethodLabel: string
  itemCount: number
  total: number
}

export interface OrderItem {
  productId: string
  productName: string
  quantity: number
  unitPrice: number
  lineTotal: number
}

export interface OrderEvent {
  type: string
  description: string
  source: string
  occurredAt: string
}

export interface Order {
  id: string
  customerId: string
  customerName: string
  createdAt: string
  status: OrderStatus
  statusLabel: string
  paymentMethod: PaymentMethod
  paymentMethodLabel: string
  notes?: string | null
  total: number
  items: OrderItem[]
  events: OrderEvent[]
  allowedNextStatuses: OrderStatus[]
}

export interface CustomerLookup {
  id: string
  name: string
  email: string
}

export interface ProductLookup {
  id: string
  name: string
  sku: string
  unitPrice: number
}

export interface CreateOrderItem {
  productId: string
  quantity: number
}

export interface CreateOrder {
  customerId: string
  status: OrderStatus
  paymentMethod: PaymentMethod
  notes?: string
  items: CreateOrderItem[]
}

export interface DailyRevenue {
  date: string
  orderCount: number
  revenue: number
}

export interface PaymentRevenue {
  paymentMethod: PaymentMethod
  paymentMethodLabel: string
  orderCount: number
  revenue: number
}

export interface RevenueReport {
  from: string
  to: string
  totalRevenue: number
  totalOrders: number
  days: DailyRevenue[]
  byPaymentMethod: PaymentRevenue[]
}

export interface StatusCount {
  status: OrderStatus
  statusLabel: string
  count: number
}

export interface DashboardSummary {
  totalRevenue: number
  totalOrders: number
  averageTicket: number
  revenueThirtyDays: number
  revenuePreviousThirtyDays: number
  ordersThirtyDays: number
  byStatus: StatusCount[]
  sparklineDays: DailyRevenue[]
  byPaymentMethod: PaymentRevenue[]
}

export interface User {
  id: string
  name: string
  email: string
  role: string
}

export interface AuthResult {
  token: string
  expiresAtUtc: string
  user: User
}

export interface OrderFilters {
  status?: OrderStatus
  search?: string
  from?: string
  to?: string
}
