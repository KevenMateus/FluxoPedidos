import { useMemo } from 'react'
import type { DailyRevenue } from '../types'

export type ChartType = 'bar' | 'line' | 'area'

const W = 760
const H = 280
const PAD = { top: 16, right: 16, bottom: 28, left: 64 }

export function RevenueChart({ data, type }: { data: DailyRevenue[]; type: ChartType }) {
  const max = useMemo(() => Math.max(...data.map((d) => d.revenue), 1), [data])

  if (data.length === 0) return <p className="muted">Sem faturamento no período.</p>

  const innerW = W - PAD.left - PAD.right
  const innerH = H - PAD.top - PAD.bottom
  const n = data.length

  const x = (i: number) => PAD.left + (n === 1 ? innerW / 2 : (i / (n - 1)) * innerW)
  const y = (v: number) => PAD.top + innerH - (v / max) * innerH

  const gridlines = [0, 0.25, 0.5, 0.75, 1].map((f) => ({
    yy: PAD.top + innerH - f * innerH,
    value: max * f,
  }))

  const linePath = data.map((d, i) => `${i === 0 ? 'M' : 'L'} ${x(i)} ${y(d.revenue)}`).join(' ')
  const areaPath = `${linePath} L ${x(n - 1)} ${PAD.top + innerH} L ${x(0)} ${PAD.top + innerH} Z`

  const xLabels = [0, Math.floor(n / 2), n - 1].filter((v, idx, a) => a.indexOf(v) === idx)

  return (
    <svg className="chart-svg" viewBox={`0 0 ${W} ${H}`} role="img" aria-label="Gráfico de faturamento">
      <defs>
        <linearGradient id="areaFill" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor="#1E88E5" stopOpacity="0.45" />
          <stop offset="100%" stopColor="#1E88E5" stopOpacity="0.02" />
        </linearGradient>
      </defs>

      {gridlines.map((g, i) => (
        <g key={i}>
          <line x1={PAD.left} y1={g.yy} x2={W - PAD.right} y2={g.yy} stroke="#E1E7F0" strokeWidth="1" />
          <text x={PAD.left - 8} y={g.yy + 4} textAnchor="end" className="chart-axis">
            {compact(g.value)}
          </text>
        </g>
      ))}

      {type === 'bar' &&
        data.map((d, i) => {
          const bw = Math.max(2, (innerW / n) * 0.6)
          const by = y(d.revenue)
          return <rect key={i} x={x(i) - bw / 2} y={by} width={bw} height={PAD.top + innerH - by} rx="2" fill="#1565C0" />
        })}

      {type === 'area' && <path d={areaPath} fill="url(#areaFill)" />}

      {(type === 'line' || type === 'area') && (
        <>
          <path d={linePath} fill="none" stroke="#1565C0" strokeWidth="2.5" />
          {data.map((d, i) => (
            <circle key={i} cx={x(i)} cy={y(d.revenue)} r="2.5" fill="#1565C0" />
          ))}
        </>
      )}

      {xLabels.map((i) => (
        <text key={i} x={x(i)} y={H - 8} textAnchor="middle" className="chart-axis">
          {new Date(data[i].date).toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit' })}
        </text>
      ))}
    </svg>
  )
}

function compact(v: number): string {
  if (v >= 1_000_000) return `R$${(v / 1_000_000).toFixed(1)}M`
  if (v >= 1_000) return `R$${(v / 1_000).toFixed(0)}k`
  return `R$${v.toFixed(0)}`
}
