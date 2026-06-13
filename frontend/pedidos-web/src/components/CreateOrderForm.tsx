import { useEffect, useMemo, useState } from 'react'
import { useCatalogStore } from '../store/catalogStore'
import { useOrdersStore } from '../store/ordersStore'
import { useToastStore } from '../store/toastStore'
import { brl } from '../format'
import type { CreateOrderItem, OrderStatus, PaymentMethod } from '../types'

interface Line extends CreateOrderItem {
  key: number
}

const PAYMENTS: { value: PaymentMethod; label: string }[] = [
  { value: 'Pix', label: 'PIX' },
  { value: 'Boleto', label: 'Boleto' },
  { value: 'CreditCard', label: 'Cartão de crédito' },
  { value: 'Cash', label: 'Dinheiro' },
]

export function CreateOrderForm() {
  const { customers, products, load } = useCatalogStore()
  const createOrder = useOrdersStore((s) => s.createOrder)
  const pushToast = useToastStore((s) => s.push)

  const [customerId, setCustomerId] = useState('')
  const [status, setStatus] = useState<OrderStatus>('Pending')
  const [payment, setPayment] = useState<PaymentMethod>('Pix')
  const [notes, setNotes] = useState('')
  const [lines, setLines] = useState<Line[]>([])
  const [submitting, setSubmitting] = useState(false)

  useEffect(() => {
    void load()
  }, [load])

  const productById = useMemo(() => new Map(products.map((p) => [p.id, p])), [products])

  const total = useMemo(
    () =>
      lines.reduce((sum, l) => {
        const p = productById.get(l.productId)
        return sum + (p ? p.unitPrice * l.quantity : 0)
      }, 0),
    [lines, productById],
  )

  const addLine = () =>
    setLines((prev) => [...prev, { key: Date.now() + prev.length, productId: products[0]?.id ?? '', quantity: 1 }])

  const updateLine = (key: number, patch: Partial<Line>) =>
    setLines((prev) => prev.map((l) => (l.key === key ? { ...l, ...patch } : l)))

  const removeLine = (key: number) => setLines((prev) => prev.filter((l) => l.key !== key))

  const canSubmit = customerId && lines.length > 0 && lines.every((l) => l.productId && l.quantity > 0)

  const submit = async () => {
    setSubmitting(true)
    try {
      await createOrder({
        customerId,
        status,
        paymentMethod: payment,
        notes: notes.trim() || undefined,
        items: lines.map((l) => ({ productId: l.productId, quantity: l.quantity })),
      })
      pushToast('success', 'Pedido criado com sucesso.')
      setCustomerId('')
      setStatus('Pending')
      setPayment('Pix')
      setNotes('')
      setLines([])
    } catch (e) {
      pushToast('error', extractError(e))
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section className="card">
      <div className="card-header">
        <h2>Novo pedido</h2>
      </div>

      <label className="field">
        <span>Cliente</span>
        <select value={customerId} onChange={(e) => setCustomerId(e.target.value)}>
          <option value="">Selecione um cliente...</option>
          {customers.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </label>

      <div className="field-row">
        <label className="field">
          <span>Status inicial</span>
          <select value={status} onChange={(e) => setStatus(e.target.value as OrderStatus)}>
            <option value="Pending">Pendente</option>
            <option value="Paid">Pago</option>
          </select>
        </label>
        <label className="field">
          <span>Forma de pagamento</span>
          <select value={payment} onChange={(e) => setPayment(e.target.value as PaymentMethod)}>
            {PAYMENTS.map((p) => (
              <option key={p.value} value={p.value}>
                {p.label}
              </option>
            ))}
          </select>
        </label>
      </div>

      <label className="field">
        <span>Observação (opcional)</span>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={2}
          maxLength={1000}
          placeholder="Ex.: entregar no período da tarde"
        />
      </label>

      <div className="lines">
        {lines.map((l) => {
          const p = productById.get(l.productId)
          return (
            <div className="line" key={l.key}>
              <select value={l.productId} onChange={(e) => updateLine(l.key, { productId: e.target.value })}>
                {products.map((prod) => (
                  <option key={prod.id} value={prod.id}>
                    {prod.name} — {brl(prod.unitPrice)}
                  </option>
                ))}
              </select>
              <input
                type="number"
                min={1}
                value={l.quantity}
                onChange={(e) => updateLine(l.key, { quantity: Math.max(1, Number(e.target.value)) })}
              />
              <span className="line-total">{p ? brl(p.unitPrice * l.quantity) : '-'}</span>
              <button className="ghost" onClick={() => removeLine(l.key)} title="Remover item">
                Remover
              </button>
            </div>
          )
        })}
      </div>

      <button className="ghost add-line" onClick={addLine} disabled={products.length === 0}>
        Adicionar item
      </button>

      <div className="form-footer">
        <div className="total-box">
          Total: <strong>{brl(total)}</strong>
        </div>
        <button className="primary" disabled={!canSubmit || submitting} onClick={submit}>
          {submitting ? 'Salvando...' : 'Criar pedido'}
        </button>
      </div>
    </section>
  )
}

function extractError(e: unknown): string {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const resp = (e as { response?: { data?: { detail?: string } } }).response
    if (resp?.data?.detail) return resp.data.detail
  }
  return 'Falha ao criar o pedido.'
}
