import { useState } from 'react'
import { jsPDF } from 'jspdf'
import autoTable from 'jspdf-autotable'
import { reportsApi } from '../api/client'
import { useToastStore } from '../store/toastStore'
import { brl } from '../format'
import { RevenueChart, type ChartType } from './RevenueChart'
import type { RevenueReport as Report } from '../types'

function isoDaysAgo(days: number): string {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return d.toISOString().slice(0, 10)
}

const CHART_TYPES: { value: ChartType; label: string }[] = [
  { value: 'bar', label: 'Barras' },
  { value: 'line', label: 'Linha' },
  { value: 'area', label: 'Área' },
]

export function RevenueReport() {
  const pushToast = useToastStore((s) => s.push)
  const [from, setFrom] = useState(isoDaysAgo(30))
  const [to, setTo] = useState(isoDaysAgo(0))
  const [chartType, setChartType] = useState<ChartType>('bar')
  const [report, setReport] = useState<Report | null>(null)
  const [loading, setLoading] = useState(false)

  const run = async () => {
    setLoading(true)
    try {
      setReport(await reportsApi.revenue(from, to))
    } catch {
      pushToast('error', 'Falha ao gerar o relatório.')
    } finally {
      setLoading(false)
    }
  }

  const downloadCsv = async () => {
    try {
      const blob = await reportsApi.downloadCsv(from, to)
      triggerDownload(blob, `faturamento_${from}_${to}.csv`)
      pushToast('success', 'CSV gerado pelo servidor.')
    } catch {
      pushToast('error', 'Falha ao baixar o CSV.')
    }
  }

  const downloadPdf = () => {
    if (!report) return
    const doc = new jsPDF()
    doc.setFontSize(16)
    doc.text('Relatório de Faturamento', 14, 18)
    doc.setFontSize(10)
    doc.setTextColor(100)
    doc.text(`Período: ${fmt(report.from)} a ${fmt(report.to)}`, 14, 25)
    doc.text(`Faturamento total: ${brl(report.totalRevenue)}  •  Pedidos: ${report.totalOrders}`, 14, 31)

    autoTable(doc, {
      startY: 38,
      head: [['Data', 'Pedidos', 'Faturamento']],
      body: report.days.map((d) => [fmt(d.date), String(d.orderCount), brl(d.revenue)]),
      headStyles: { fillColor: [21, 101, 192] },
      styles: { fontSize: 9 },
    })

    if (report.byPaymentMethod.length > 0) {
      autoTable(doc, {
        head: [['Forma de pagamento', 'Pedidos', 'Faturamento']],
        body: report.byPaymentMethod.map((p) => [p.paymentMethodLabel, String(p.orderCount), brl(p.revenue)]),
        headStyles: { fillColor: [0, 172, 193] },
        styles: { fontSize: 9 },
      })
    }

    doc.save(`faturamento_${from}_${to}.pdf`)
    pushToast('success', 'PDF gerado no navegador.')
  }

  const avgTicket = report && report.totalOrders > 0
    ? report.totalRevenue / report.totalOrders
    : 0

  const totalPayRev = report
    ? Math.max(...report.byPaymentMethod.map((p) => p.revenue), 1)
    : 1

  return (
    <div className="rev-root">
      {/* ── Filtros ── */}
      <div className="card rev-filters">
        <div className="rev-filters-inner">
          <label className="field compact">
            <span>De</span>
            <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
          </label>
          <label className="field compact">
            <span>Até</span>
            <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
          </label>
          <button className="primary" onClick={run} disabled={loading}>
            {loading ? 'Calculando...' : 'Gerar relatório'}
          </button>
        </div>
        {report && (
          <div className="rev-actions">
            <button className="ghost small" onClick={downloadCsv}>Baixar CSV</button>
            <button className="ghost small" onClick={downloadPdf}>Baixar PDF</button>
          </div>
        )}
      </div>

      {report && (
        <>
          {/* ── KPIs ── */}
          <div className="db-kpis">
            <div className="db-kpi db-kpi--blue">
              <span className="db-kpi-label">Faturamento total</span>
              <span className="db-kpi-value">{brl(report.totalRevenue)}</span>
              <span className="db-kpi-sub">{fmt(report.from)} — {fmt(report.to)}</span>
            </div>
            <div className="db-kpi db-kpi--navy">
              <span className="db-kpi-label">Pedidos no período</span>
              <span className="db-kpi-value">{report.totalOrders.toLocaleString('pt-BR')}</span>
              <span className="db-kpi-sub">{report.days.length} dias com vendas</span>
            </div>
            <div className="db-kpi db-kpi--cyan">
              <span className="db-kpi-label">Ticket médio</span>
              <span className="db-kpi-value">{brl(avgTicket)}</span>
              <span className="db-kpi-sub">por pedido no período</span>
            </div>
          </div>

          {/* ── Gráfico ── */}
          <div className="card">
            <div className="chart-toolbar">
              <span className="db-section-title" style={{ marginBottom: 0 }}>Faturamento por dia</span>
              <div className="segmented" style={{ marginLeft: 'auto' }}>
                {CHART_TYPES.map((c) => (
                  <button
                    key={c.value}
                    className={chartType === c.value ? 'active' : ''}
                    onClick={() => setChartType(c.value)}
                  >
                    {c.label}
                  </button>
                ))}
              </div>
            </div>
            <RevenueChart data={report.days} type={chartType} />
          </div>

          {/* ── Por forma de pagamento ── */}
          {report.byPaymentMethod.length > 0 && (
            <div className="card db-payment-card">
              <div className="db-section-title">Por forma de pagamento</div>
              <div className="db-payment-list">
                {report.byPaymentMethod.map((p) => (
                  <div className="db-payment-row" key={p.paymentMethod}>
                    <span className="db-payment-label">{p.paymentMethodLabel}</span>
                    <div className="db-payment-track">
                      <div className="db-payment-fill" style={{ width: `${(p.revenue / totalPayRev) * 100}%` }} />
                    </div>
                    <span className="db-payment-revenue">{brl(p.revenue)}</span>
                    <span className="db-payment-orders">{p.orderCount.toLocaleString('pt-BR')} pedidos</span>
                    <span className="db-payment-pct">{((p.revenue / report.totalRevenue) * 100).toFixed(1)}%</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}

const fmt = (iso: string) => new Date(iso).toLocaleDateString('pt-BR')

function triggerDownload(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = filename
  a.click()
  URL.revokeObjectURL(url)
}
