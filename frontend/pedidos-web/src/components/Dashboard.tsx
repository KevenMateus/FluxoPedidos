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
    return () => { active = false }
  }, [refreshKey])

  if (loading && !data) return <section className="card db-loading">Carregando indicadores...</section>
  if (!data) return null

  const trend = data.revenuePreviousThirtyDays > 0
    ? ((data.revenueThirtyDays - data.revenuePreviousThirtyDays) / data.revenuePreviousThirtyDays) * 100
    : null

  const totalStatusCount = data.byStatus.reduce((s, b) => s + b.count, 0)
  const totalPaymentRevenue = data.byPaymentMethod.reduce((s, b) => s + b.revenue, 0)

  return (
    <div className="db-root">
      {/* ── KPIs ── */}
      <div className="db-kpis">
        <div className="db-kpi db-kpi--blue">
          <span className="db-kpi-label">Faturamento total</span>
          <span className="db-kpi-value">{brl(data.totalRevenue)}</span>
          <span className="db-kpi-sub">{data.totalOrders.toLocaleString('pt-BR')} pedidos no total</span>
        </div>

        <div className="db-kpi db-kpi--navy">
          <span className="db-kpi-label">Últimos 30 dias</span>
          <span className="db-kpi-value">{brl(data.revenueThirtyDays)}</span>
          {trend !== null && (
            <span className={`db-kpi-trend ${trend >= 0 ? 'db-trend-up' : 'db-trend-down'}`}>
              {trend >= 0 ? '▲' : '▼'} {Math.abs(trend).toFixed(1)}% vs período anterior
            </span>
          )}
        </div>

        <div className="db-kpi db-kpi--cyan">
          <span className="db-kpi-label">Ticket médio</span>
          <span className="db-kpi-value">{brl(data.averageTicket)}</span>
          <span className="db-kpi-sub">{data.ordersThirtyDays.toLocaleString('pt-BR')} pedidos em 30d</span>
        </div>
      </div>

      {/* ── Sparkline + Status ── */}
      <div className="db-mid">
        <div className="card db-sparkline-card">
          <div className="db-section-title">Faturamento — últimos 30 dias</div>
          <Sparkline days={data.sparklineDays} />
          <div className="db-sparkline-legend">
            <span>
              {data.sparklineDays.length > 0
                ? new Date(data.sparklineDays[0].date).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })
                : ''}
            </span>
            <span>
              {data.sparklineDays.length > 0
                ? new Date(data.sparklineDays[data.sparklineDays.length - 1].date).toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })
                : ''}
            </span>
          </div>
        </div>

        <div className="card db-status-card">
          <div className="db-section-title">Pedidos por status</div>
          <div className="db-status-list">
            {data.byStatus.map((s) => {
              const pct = totalStatusCount > 0 ? (s.count / totalStatusCount) * 100 : 0
              return (
                <div key={s.status} className="db-status-row">
                  <span className={`badge ${statusClass(s.status)}`}>{s.statusLabel}</span>
                  <div className="db-progress-track">
                    <div className="db-progress-fill" style={{ width: `${pct}%` }} data-status={s.status} />
                  </div>
                  <span className="db-status-count">{s.count.toLocaleString('pt-BR')}</span>
                  <span className="db-status-pct">{pct.toFixed(0)}%</span>
                </div>
              )
            })}
          </div>
        </div>
      </div>

      {/* ── Forma de pagamento ── */}
      <div className="card db-payment-card">
        <div className="db-section-title">Forma de pagamento</div>
        <div className="db-payment-list">
          {data.byPaymentMethod.map((p) => {
            const pct = totalPaymentRevenue > 0 ? (p.revenue / totalPaymentRevenue) * 100 : 0
            return (
              <div key={p.paymentMethod} className="db-payment-row">
                <span className="db-payment-label">{p.paymentMethodLabel}</span>
                <div className="db-payment-track">
                  <div className="db-payment-fill" style={{ width: `${pct}%` }} />
                </div>
                <span className="db-payment-revenue">{brl(p.revenue)}</span>
                <span className="db-payment-orders">{p.orderCount.toLocaleString('pt-BR')} pedidos</span>
                <span className="db-payment-pct">{pct.toFixed(1)}%</span>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

function Sparkline({ days }: { days: DashboardSummary['sparklineDays'] }) {
  if (days.length < 2) {
    return <div className="db-sparkline-empty">Sem dados suficientes</div>
  }

  const W = 600
  const H = 120
  const PAD = { top: 10, right: 8, bottom: 4, left: 8 }
  const innerW = W - PAD.left - PAD.right
  const innerH = H - PAD.top - PAD.bottom

  const max = Math.max(...days.map((d) => d.revenue))
  const min = Math.min(...days.map((d) => d.revenue))
  const range = max - min || 1

  const xs = days.map((_, i) => PAD.left + (i / (days.length - 1)) * innerW)
  const ys = days.map((d) => PAD.top + innerH - ((d.revenue - min) / range) * innerH)

  const linePath = xs.map((x, i) => `${i === 0 ? 'M' : 'L'}${x},${ys[i]}`).join(' ')
  const areaPath = `${linePath} L${xs[xs.length - 1]},${H - PAD.bottom} L${xs[0]},${H - PAD.bottom} Z`

  const peakIdx = days.reduce((best, d, i) => (d.revenue > days[best].revenue ? i : best), 0)

  return (
    <svg viewBox={`0 0 ${W} ${H}`} className="db-sparkline-svg" preserveAspectRatio="none">
      <defs>
        <linearGradient id="spark-grad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="var(--blue)" stopOpacity="0.25" />
          <stop offset="100%" stopColor="var(--blue)" stopOpacity="0.02" />
        </linearGradient>
      </defs>
      <path d={areaPath} fill="url(#spark-grad)" />
      <path d={linePath} fill="none" stroke="var(--blue)" strokeWidth="2" strokeLinejoin="round" strokeLinecap="round" />
      <circle cx={xs[peakIdx]} cy={ys[peakIdx]} r="4" fill="var(--blue)" />
      <circle cx={xs[peakIdx]} cy={ys[peakIdx]} r="4" fill="var(--blue)" opacity="0.3">
        <animate attributeName="r" from="4" to="10" dur="1.5s" repeatCount="indefinite" />
        <animate attributeName="opacity" from="0.3" to="0" dur="1.5s" repeatCount="indefinite" />
      </circle>
    </svg>
  )
}
