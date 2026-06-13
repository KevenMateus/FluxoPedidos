import { useEffect, useState } from 'react'
import { reportsApi } from '../api/client'
import { brl } from '../format'
import { statusClass } from '../format'
import type { DashboardSummary } from '../types'

export function Dashboard({ refreshKey }: { refreshKey: number }) {
  const [data, setData] = useState<DashboardSummary | null>(null)
  const [loading, setLoading] = useState(false)

  useEffect(() => {
    let active = true
    setLoading(true)
    reportsApi
      .dashboard()
      .then((d) => active && setData(d))
      .finally(() => active && setLoading(false))
    return () => {
      active = false
    }
  }, [refreshKey])

  if (loading && !data) return <section className="card">Carregando indicadores...</section>
  if (!data) return null

  return (
    <section className="card">
      <div className="card-header">
        <h2>Indicadores</h2>
      </div>

      <div className="kpis">
        <div className="kpi">
          <span className="kpi-label">Faturamento total</span>
          <span className="kpi-value">{brl(data.totalRevenue)}</span>
        </div>
        <div className="kpi">
          <span className="kpi-label">Pedidos</span>
          <span className="kpi-value">{data.totalOrders.toLocaleString('pt-BR')}</span>
        </div>
        <div className="kpi">
          <span className="kpi-label">Ticket médio</span>
          <span className="kpi-value">{brl(data.averageTicket)}</span>
        </div>
      </div>

      <div className="status-grid">
        {data.byStatus.map((s) => (
          <div key={s.status} className="status-cell">
            <span className={statusClass(s.status)}>{s.statusLabel}</span>
            <span className="status-count">{s.count.toLocaleString('pt-BR')}</span>
          </div>
        ))}
      </div>
    </section>
  )
}
