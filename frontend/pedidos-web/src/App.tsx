import { useEffect, useState } from 'react'
import { NOTIFICATIONS_URL } from './api/client'
import { useAuthStore } from './store/authStore'
import { useOrdersStore } from './store/ordersStore'
import { useToastStore } from './store/toastStore'
import { brl } from './format'
import { Login } from './components/Login'
import { Dashboard } from './components/Dashboard'
import { OrdersList } from './components/OrdersList'
import { CreateOrderForm } from './components/CreateOrderForm'
import { RevenueReport } from './components/RevenueReport'
import { Toasts } from './components/Toasts'

type Tab = 'dashboard' | 'orders' | 'revenue'

export default function App() {
  const { token, user, restore, logout } = useAuthStore()
  const [tab, setTab] = useState<Tab>('dashboard')
  const [refreshKey, setRefreshKey] = useState(0)
  const pushToast = useToastStore((s) => s.push)
  const fetchOrders = useOrdersStore((s) => s.fetch)

  useEffect(() => {
    void restore()
  }, [restore])

  useEffect(() => {
    if (!token) return
    const es = new EventSource(`${NOTIFICATIONS_URL}/stream`)
    es.addEventListener('order-created', (ev) => {
      try {
        const data = JSON.parse((ev as MessageEvent).data)
        pushToast('info', `Novo pedido de ${data.customerName} — ${brl(data.total)} (faixa ${data.tier})`)
        setRefreshKey((k) => k + 1)
        void fetchOrders(1)
      } catch {
        /* ignora frames malformados */
      }
    })
    es.onerror = () => {
      /* EventSource reconecta sozinho */
    }
    return () => es.close()
  }, [token, pushToast, fetchOrders])

  if (!token) return <Login />

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="brand-mark">Pedidos</span>
        </div>
        <nav className="tabs">
          <button className={tab === 'dashboard' ? 'active' : ''} onClick={() => setTab('dashboard')}>
            Dashboard
          </button>
          <button className={tab === 'orders' ? 'active' : ''} onClick={() => setTab('orders')}>
            Pedidos
          </button>
          <button className={tab === 'revenue' ? 'active' : ''} onClick={() => setTab('revenue')}>
            Faturamento
          </button>
        </nav>
        <div className="user-box">
          <span className="user-name">{user?.name}</span>
          <span className="user-role">{user?.role}</span>
          <button className="ghost light" onClick={logout}>Sair</button>
        </div>
      </header>

      <main className="content">
        {tab === 'dashboard' && <Dashboard refreshKey={refreshKey} />}
        {tab === 'orders' && (
          <div className="grid">
            <CreateOrderForm />
            <OrdersList />
          </div>
        )}
        {tab === 'revenue' && <RevenueReport />}
      </main>

      <Toasts />
    </div>
  )
}
