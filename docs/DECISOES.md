# Decisões de Projeto — Pedidos

Documento que registra **tudo o que foi feito e o porquê**, na ordem em que as decisões
importam. Complementa o `README.md` (como rodar/o quê) com o **racional** (por quê).

---

## 1. Visão geral e objetivo

Sistema de Pedidos com domínio propositalmente simples. O valor está na **estrutura**:
backend .NET em Clean Architecture + Vertical Slices, frontend React + Zustand, banco
relacional (PostgreSQL), microserviço Node e orquestração via Docker Compose. Tudo
assíncrono, com Swagger, DI, DTOs, testes de serviço e SOLID.

---

## 2. Stack escolhida e por quê

| Camada | Escolha | Por quê |
|--------|---------|---------|
| Backend | .NET 9 / ASP.NET Core | Exigido. Suporte first-class a async, DI nativa, EF Core e Dapper convivendo bem. |
| ORM/Acesso | EF Core **+** Dapper | Exigido usar ambos. Divisão por tipo de carga (ver §5). |
| Banco | PostgreSQL 16 | Relacional, robusto em agregações, e o Npgsql tem **`COPY` binário** para popular volume rápido — decisivo para o requisito de performance. |
| Frontend | React + Vite + TypeScript | Exigido React. Vite = build/dev rápido; TS = segurança de tipos espelhando os DTOs. |
| Estado (front) | **Zustand** | Exigido. API mínima, sem boilerplate de reducers; store por domínio (orders/catalog). |
| Microserviço | Node + Express | Exigido Node. Express é o caminho mais direto para um worker HTTP simples. |
| Orquestração | Docker Compose | Exigido subir tudo junto: db + api + web + notification. |

---

## 3. Arquitetura: Clean + Vertical Slices

**Por que Clean Architecture:** isola o núcleo (Domain) de detalhes (banco, web, HTTP).
As dependências apontam para dentro; a Application define interfaces (portas) e a
Infrastructure as implementa (Inversão de Dependência).

**Por que Vertical Slices por cima:** em vez de pastas horizontais gigantes
(`Services/`, `Repositories/` com tudo junto), cada caso de uso vive na sua fatia
(`Orders/`, `Reports/`, `Catalog/`, `Notifications/`). Benefícios: coesão alta,
acoplamento baixo, contexto local (bom para humanos **e** IA — ver `AI_NOTES.md`).

### Projetos

- **Pedidos.Domain** — entidades (`Order`, `OrderItem`, `Customer`, `Product`) e suas
  invariantes. Sem dependência de framework.
- **Pedidos.Application** — serviços (casos de uso), DTOs e interfaces. Só conhece
  abstrações.
- **Pedidos.Infrastructure** — EF Core (DbContext, configs, escrita), Dapper (leitura
  pesada), seeder (`COPY`), notificador HTTP.
- **Pedidos.Api** — controllers, Swagger, CORS, middleware de exceção, seed no startup.
- **Pedidos.Tests** — testes unitários da camada de serviço.

---

## 4. Regras de design exigidas — como cumpri cada uma

- **"Service só conversa com o seu repositório e com outros services, jamais com outro
  repositório."**
  `OrderService` depende de `IOrderReadRepository` + `IOrderWriteRepository` (ambos do
  agregado Pedido) e da porta `IOrderCreatedNotifier`. Não acessa o repositório de
  Catalog nem o de Reports. Se precisasse de dados de outro slice, chamaria o **service**
  daquele slice.

- **"Services só trafegam DTO, nada de entidade."**
  Decisão estrutural: **os repositórios projetam direto para DTO**. `OrderDto`,
  `OrderListItemDto`, etc. saem já prontos da Infrastructure. A entidade de domínio
  fica confinada a Domain + Infrastructure e **nunca** aparece numa assinatura de
  serviço. A regra deixa de ser "convenção" e passa a ser garantida pelo tipo.

- **Injeção de dependência.** `AddApplication()` e `AddInfrastructure(config)`
  registram tudo; construtores recebem abstrações. Nenhum `new` de dependência dentro
  de serviço.

- **`summary`.** Interfaces, DTOs e endpoints documentados; Swagger lê os XMLs de
  `Pedidos.Api` e `Pedidos.Application`.

- **Tudo async.** Toda a cadeia (controller → service → repositório → ADO/EF) é
  `async`/`await` com `CancellationToken` propagado de ponta a ponta.

- **SOLID.** Detalhado no README (§"Princípios SOLID na prática").

---

## 5. EF Core vs. Dapper — a decisão central de acesso a dados

Critério: **EF para escrita e leitura simples; Dapper para leitura pesada.**

### Escrita → EF Core (`OrderWriteRepository`)
Criar pedido é transacional (1 order + N items), exige validar FKs (cliente/produto
existem?) e se beneficia do change tracking e do INSERT em lote do `SaveChanges`. O
preço unitário é **resolvido no servidor** a partir do produto (snapshot), nunca
informado pelo cliente — evita adulteração.

### Leitura simples → EF Core + LINQ (`CatalogReadRepository`)
Listar clientes/produtos para os seletores da UI: `AsNoTracking().Select(→ DTO)`. Sem
agregação, LINQ é mais legível e seguro que SQL manual.

### Leitura pesada → Dapper (`OrderReadRepository`, `RevenueRepository`)
- **Listagem paginada de pedidos com total:** consulta sobre milhares de pedidos. O SQL
  pagina os pedidos pelo índice de `created_at` num subquery e só então agrega os itens
  da página com `LEFT JOIN LATERAL` — evitando agregar a tabela inteira a cada página.
- **Faturamento por dia:** `GROUP BY` por dia cruzando orders × order_items, filtrando
  pelo índice de `created_at`. Mapeamento sem tracking, SQL explícito.

Detalhe técnico aprendido: `COUNT(*)` no Postgres é `bigint` — as contagens são
castadas para `::int` no SQL para casar com os DTOs (ver `AI_NOTES.md`).

---

## 6. Modelo de dados

```
customers (id, name, email[unique], created_at)
products  (id, name, sku[unique], unit_price)
orders    (id, customer_id → customers, created_at, status)
order_items (id, order_id → orders, product_id → products,
             product_name, quantity, unit_price)
```

Decisões:
- **`order_items.product_name` e `unit_price` são snapshots** do momento da venda. Se o
  preço/nome do produto mudar depois, pedidos antigos permanecem corretos.
- **Total não é coluna** — é derivado da soma das linhas (no domínio e no SQL). Evita
  inconsistência entre o total armazenado e os itens.
- **Índices**: `orders.created_at` (relatório por período e ordenação da listagem),
  `orders.customer_id`, `order_items.order_id` (FKs/joins). São o que faz a
  performance "importar" e melhorar com volume.
- **Status como enum** (`Pending/Paid/Shipped/Cancelled`) persistido como `int`.

---

## 7. Seed automático e geração de volume

- **Automático no startup**, idempotente, com retry de readiness do banco — para o
  docker-compose funcionar sem orquestração manual. Cria schema + massa se vazio.
- **`Bogus`** para dados realistas (nomes, e-mails, produtos, preços), com **seed fixo**
  → massa reprodutível entre execuções.
- **`COPY` binário do Npgsql** para inserir pedidos/itens em lote. Resultado medido:
  ~5.000 pedidos + ~17,5k itens em ~1,1s. INSERT um a um seria inviável para o volume.
- Datas dos pedidos **espalhadas em 365 dias** para o relatório de período ter sentido.
- Endpoint `POST /api/seed/orders?count=N` (até 1M) para gerar carga sob demanda.

---

## 8. Microserviço e comunicação

**Implementado:** HTTP best-effort. `OrderService` → `IOrderCreatedNotifier` →
`HttpOrderCreatedNotifier` faz `POST /events/order-created` ao Node. Falha do
microserviço **não** desfaz o pedido (logada apenas). O Node enriquece (faixa de valor)
e mantém métricas (`/stats`).

**Por que HTTP agora:** simples, demonstrável, sobe junto no compose.

**Como seria em produção:** Outbox + fila (RabbitMQ/SQS/Kafka) + consumidor idempotente
— desacopla disponibilidade e latência, processamento assíncrono e escalável. Trocar
exigiria só uma nova implementação de `IOrderCreatedNotifier`. Detalhado no README.

---

## 9. Frontend

- **Zustand**: `ordersStore` (lista, paginação, criar) e `catalogStore` (clientes/
  produtos, carregados sob demanda e cacheados).
- **Axios** isolado em `api/client.ts`; `VITE_API_URL` define a base (build-time).
- Tela com abas: **Pedidos** (form de criação + lista paginada lado a lado) e
  **Faturamento** (filtro de datas + KPIs + gráfico de barras simples por dia).
- Tema escuro, sem dependência de UI lib — CSS próprio, enxuto.

---

## 10. Tratamento de erros

Middleware único (`ExceptionHandlingMiddleware`) traduz exceções em **ProblemDetails**:
`NotFoundException`/`ArgumentException`/`InvalidOperationException` → 400 com `detail`;
demais → 500 genérico (logado). Controllers ficam limpos de try/catch.

---

## 11. Testes

xUnit + Moq + FluentAssertions, cobrindo **toda a camada de serviço** (17 testes):
normalização de paginação, delegação a repositórios, disparo da notificação,
comportamento best-effort em falha de notificação, validação de período e clamps. A
camada de serviço é a que concentra regra de orquestração — por isso o foco do teste
unitário ali, com os repositórios mockados.

---

## 12. O que eu faria com mais tempo

- **Fila/outbox** para o evento de pedido criado (hoje HTTP).
- **Migrations** versionadas em vez de `EnsureCreated` (suficiente para o desafio).
- **Testes de integração** (WebApplicationFactory + Testcontainers) batendo no Postgres
  real — o `Program` já é `partial` preparado para isso.
- **Cursor-based pagination** na listagem (keyset) para escalar melhor que OFFSET em
  páginas muito profundas.
- **Cache** no relatório de faturamento (dados históricos mudam pouco).
