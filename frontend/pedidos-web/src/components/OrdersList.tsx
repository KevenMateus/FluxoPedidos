import { Fragment, useEffect, useState } from 'react'
import { useOrdersStore } from '../store/ordersStore'
import { useToastStore } from '../store/toastStore'
import { brl, dateTime, statusClass } from '../format'
import type { OrderStatus } from '../types'

const STATUS_OPTIONS: { value: OrderStatus | ''; label: string }[] = [
  { value: '', label: 'Todos os status' },
  { value: 'Pending', label: 'Pendente' },
  { value: 'Paid', label: 'Pago' },
  { value: 'Shipped', label: 'Enviado' },
  { value: 'Delivered', label: 'Entregue' },
  { value: 'Cancelled', label: 'Cancelado' },
]

const STATUS_LABEL: Record<OrderStatus, string> = {
  Pending: 'Pendente',
  Paid: 'Pago',
  Shipped: 'Enviado',
  Delivered: 'Entregue',
  Cancelled: 'Cancelado',
}

export function OrdersList() {
  const {
    items, page, totalPages, totalCount, pageSize, loading, error,
    expandedId, fetch, setPageSize, setFilters, toggleExpand,
  } = useOrdersStore()

  const [status, setStatus] = useState<OrderStatus | ''>('')
  const [search, setSearch] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')

  useEffect(() => {
    void fetch(1)
  }, [])

  const applyFilters = () =>
    setFilters({
      status: status || undefined,
      search: search.trim() || undefined,
      from: from || undefined,
      to: to || undefined,
    })

  const clearFilters = () => {
    setStatus('')
    setSearch('')
    setFrom('')
    setTo('')
    setFilters({})
  }

  return (
    <section className="card">
      <div className="card-header">
        <h2>Pedidos</h2>
        <span className="muted">{totalCount.toLocaleString('pt-BR')} no total</span>
      </div>

      <div className="filters">
        <label className="field compact">
          <span>Status</span>
          <select value={status} onChange={(e) => setStatus(e.target.value as OrderStatus | '')}>
            {STATUS_OPTIONS.map((o) => (
              <option key={o.value} value={o.value}>{o.label}</option>
            ))}
          </select>
        </label>
        <label className="field compact grow">
          <span>Cliente</span>
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Buscar por nome" />
        </label>
        <label className="field compact">
          <span>De</span>
          <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </label>
        <label className="field compact">
          <span>Até</span>
          <input type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </label>
        <button className="primary" onClick={applyFilters}>Filtrar</button>
        <button className="ghost" onClick={clearFilters}>Limpar</button>
      </div>

      {error && <p className="error">{error}</p>}

      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th></th>
              <th>Cliente</th>
              <th>Criado em</th>
              <th>Pagamento</th>
              <th>Status</th>
              <th className="num">Itens</th>
              <th className="num">Total</th>
            </tr>
          </thead>
          <tbody>
            {items.map((o) => (
              <Fragment key={o.id}>
                <tr className={`clickable ${expandedId === o.id ? 'expanded' : ''}`} onClick={() => toggleExpand(o.id)}>
                  <td className="expander">{expandedId === o.id ? '−' : '+'}</td>
                  <td>{o.customerName}</td>
                  <td>{dateTime(o.createdAt)}</td>
                  <td>{o.paymentMethodLabel}</td>
                  <td><span className={statusClass(o.status)}>{o.statusLabel}</span></td>
                  <td className="num">{o.itemCount}</td>
                  <td className="num strong">{brl(o.total)}</td>
                </tr>
                {expandedId === o.id && (
                  <tr className="detail-row">
                    <td colSpan={7}>
                      <OrderDetail />
                    </td>
                  </tr>
                )}
              </Fragment>
            ))}
            {!loading && items.length === 0 && (
              <tr><td colSpan={7} className="muted center">Nenhum pedido encontrado.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      <div className="pagination">
        <button disabled={page <= 1 || loading} onClick={() => fetch(page - 1)}>Anterior</button>
        <span>Página {page} de {Math.max(totalPages, 1)}</span>
        <button disabled={page >= totalPages || loading} onClick={() => fetch(page + 1)}>Próxima</button>
        <label className="page-size">
          <span>Por página</span>
          <select value={pageSize} onChange={(e) => setPageSize(Number(e.target.value))}>
            {[10, 20, 50, 100].map((n) => <option key={n} value={n}>{n}</option>)}
          </select>
        </label>
        {loading && <span className="muted">carregando...</span>}
      </div>
    </section>
  )
}

function OrderDetail() {
  const { expandedOrder, expandedLoading, changeStatus } = useOrdersStore()
  const pushToast = useToastStore((s) => s.push)
  const [note, setNote] = useState('')
  const [busy, setBusy] = useState(false)

  if (expandedLoading || !expandedOrder) return <div className="detail muted">Carregando detalhes...</div>
  const order = expandedOrder

  const apply = async (status: OrderStatus) => {
    setBusy(true)
    try {
      await changeStatus(order.id, status, note.trim() || undefined)
      pushToast('success', `Status alterado para ${STATUS_LABEL[status]}.`)
      setNote('')
    } catch (e) {
      pushToast('error', extractError(e))
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="detail">
      <div className="detail-grid">
        <div>
          <h4>Itens</h4>
          <table className="inner-table">
            <thead>
              <tr><th>Produto</th><th className="num">Qtd</th><th className="num">Unit.</th><th className="num">Subtotal</th></tr>
            </thead>
            <tbody>
              {order.items.map((i, idx) => (
                <tr key={idx}>
                  <td>{i.productName}</td>
                  <td className="num">{i.quantity}</td>
                  <td className="num">{brl(i.unitPrice)}</td>
                  <td className="num">{brl(i.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {order.notes && <p className="notes"><strong>Observação:</strong> {order.notes}</p>}
        </div>

        <div>
          <h4>Histórico</h4>
          <ul className="timeline">
            {order.events.map((e, idx) => (
              <li key={idx} className={`tl tl-${e.source}`}>
                <span className="tl-time">{dateTime(e.occurredAt)}</span>
                <span className="tl-desc">{e.description}</span>
                <span className="tl-source">{e.source}</span>
              </li>
            ))}
          </ul>
        </div>
      </div>

      <div className="detail-actions">
        {order.allowedNextStatuses.length > 0 ? (
          <>
            <input
              className="note-input"
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Observação ao mudar status (opcional)"
            />
            {order.allowedNextStatuses.map((s) => (
              <button key={s} className="primary small" disabled={busy} onClick={() => apply(s)}>
                {STATUS_LABEL[s]}
              </button>
            ))}
          </>
        ) : (
          <span className="muted">Pedido em status final ({order.statusLabel}).</span>
        )}
      </div>
    </div>
  )
}

function extractError(e: unknown): string {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const resp = (e as { response?: { data?: { detail?: string } } }).response
    if (resp?.data?.detail) return resp.data.detail
  }
  return 'Falha na operação.'
}
