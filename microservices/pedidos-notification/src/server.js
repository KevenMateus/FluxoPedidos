import express from 'express'

const app = express()
app.use(express.json({ limit: '1mb' }))

const PORT = process.env.PORT || 3001
const API_URL = process.env.API_URL || 'http://localhost:5005'
const SERVICE_TOKEN = process.env.SERVICE_TOKEN || 'service-token-interno-troque-em-producao'

app.use((_req, res, next) => {
  res.setHeader('Access-Control-Allow-Origin', '*')
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type')
  next()
})

const stats = {
  ordersProcessed: 0,
  revenueSeen: 0,
  enrichmentsWritten: 0,
  enrichmentsFailed: 0,
  sseClients: 0,
  lastOrderId: null,
  startedAt: new Date().toISOString(),
}

const sseClients = new Set()

function broadcast(event, payload) {
  const frame = `event: ${event}\ndata: ${JSON.stringify(payload)}\n\n`
  for (const res of sseClients) res.write(frame)
}

app.get('/stream', (req, res) => {
  res.writeHead(200, {
    'Content-Type': 'text/event-stream',
    'Cache-Control': 'no-cache',
    Connection: 'keep-alive',
  })
  res.write(`event: connected\ndata: ${JSON.stringify({ ok: true })}\n\n`)

  sseClients.add(res)
  stats.sseClients = sseClients.size

  const heartbeat = setInterval(() => res.write(': keep-alive\n\n'), 25000)

  req.on('close', () => {
    clearInterval(heartbeat)
    sseClients.delete(res)
    stats.sseClients = sseClients.size
  })
})

app.get('/health', (_req, res) => res.json({ status: 'ok' }))
app.get('/stats', (_req, res) => res.json(stats))

app.post('/events/order-created', async (req, res) => {
  const order = req.body
  if (!order || !order.id) {
    return res.status(400).json({ error: 'payload inválido: faltando id do pedido' })
  }

  const total = Number(order.total) || 0
  const tier = total >= 1000 ? 'alto' : total >= 200 ? 'medio' : 'baixo'
  const risk = Math.min(95, Math.round((total % 100) + (tier === 'alto' ? 10 : 0)))

  stats.ordersProcessed += 1
  stats.revenueSeen += total
  stats.lastOrderId = order.id

  console.log(`[notification] pedido ${order.id} | ${order.customerName} | total=${total} | faixa=${tier} | risco=${risk}%`)

  broadcast('order-created', {
    orderId: order.id,
    customerName: order.customerName,
    total,
    tier,
    at: new Date().toISOString(),
  })

  res.status(202).json({ received: true, orderId: order.id, tier, risk })

  const description = `Enriquecido pelo microserviço: faixa de valor "${tier}", score de risco ${risk}%.`
  writeEnrichmentWithRetry(order.id, description).catch(() => {})
})

async function writeEnrichmentWithRetry(orderId, description, attempts = 3) {
  for (let i = 1; i <= attempts; i++) {
    try {
      const resp = await fetch(`${API_URL}/api/internal/orders/${orderId}/enrichment`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'X-Service-Token': SERVICE_TOKEN },
        body: JSON.stringify({ description }),
      })
      if (resp.ok) {
        stats.enrichmentsWritten += 1
        console.log(`[notification] timeline atualizada no pedido ${orderId}`)
        return
      }
      throw new Error(`HTTP ${resp.status}`)
    } catch (err) {
      if (i === attempts) {
        stats.enrichmentsFailed += 1
        console.warn(`[notification] falha ao gravar timeline do pedido ${orderId} após ${attempts} tentativas: ${err.message}`)
        return
      }
      await new Promise((r) => setTimeout(r, 500 * i))
    }
  }
}

app.listen(PORT, () => {
  console.log(`pedidos-notification ouvindo na porta ${PORT} (API=${API_URL})`)
})
