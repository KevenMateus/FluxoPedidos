# Pedidos

Sistema de **Pedidos** construído do zero como desafio técnico. O domínio é simples
de propósito — o foco está em **como** o projeto é estruturado: Clean Architecture +
Vertical Slices no backend .NET, React + Zustand no frontend, PostgreSQL, um
microserviço Node para processar o evento de "pedido criado", e tudo orquestrado por
um único `docker compose up`.

> **TL;DR:** `cd Pedidos && docker compose up --build -d` → abra http://localhost:5173
> (UI) e http://localhost:5005/swagger (API). O banco, o schema e ~5.000 pedidos de
> exemplo são criados **automaticamente** no primeiro start.

---

## Sumário

- [Como rodar](#como-rodar)
- [O que foi entregue](#o-que-foi-entregue)
- [Arquitetura](#arquitetura)
- [EF Core vs. Dapper — onde e por quê](#ef-core-vs-dapper--onde-e-por-quê)
- [Seed automático e geração de volume](#seed-automático-e-geração-de-volume)
- [O microserviço Node](#o-microserviço-node)
- [Endpoints](#endpoints)
- [Princípios SOLID na prática](#princípios-solid-na-prática)
- [Testes](#testes)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Documentos relacionados](#documentos-relacionados)

---

## Como rodar

### Opção A — Docker (recomendada, sobe tudo)

Pré-requisito: Docker Desktop em execução.

```bash
cd Pedidos
docker compose up --build -d
```

Sobe quatro contêineres:

| Serviço | URL | Descrição |
|---------|-----|-----------|
| `web` | http://localhost:5173 | SPA React (nginx) |
| `api` | http://localhost:5005 · `/swagger` | API .NET 9 |
| `notification` | http://localhost:3001 · `/stats` | Microserviço Node |
| `db` | localhost:5432 | PostgreSQL 16 |

No primeiro start a API **cria o banco, o schema e popula ~5.000 pedidos sozinha**
(ver [Seed automático](#seed-automático-e-geração-de-volume)). Acompanhe com
`docker compose logs -f api`.

> Se o build do Docker reclamar de "No such image" para imagens recém-construídas,
> rode `BUILDX_NO_DEFAULT_ATTESTATIONS=1 docker compose build` antes do `up`. É um
> comportamento do buildx ao exportar manifestos com atestação — detalhado em
> [`AI_NOTES.md`](AI_NOTES.md).

### Opção B — Local (dev)

```bash
# 1. Banco
docker run -d --name pedidos-db -e POSTGRES_DB=pedidos -e POSTGRES_USER=pedidos \
  -e POSTGRES_PASSWORD=pedidos -p 5432:5432 postgres:16-alpine

# 2. API (.NET 9) — cria schema + seed no startup
cd backend
dotnet run --project Pedidos.Api        # http://localhost:5005/swagger

# 3. Microserviço
cd microservices/pedidos-notification && npm install && npm start   # :3001

# 4. Frontend
cd frontend/pedidos-web && npm install && npm run dev   # http://localhost:5173
```

---

## O que foi entregue

As quatro funcionalidades mínimas, todas **assíncronas** de ponta a ponta:

1. **Listar pedidos** — `GET /api/orders` paginado, com itens e total de cada pedido.
2. **Faturamento por período** — `GET /api/reports/revenue` agregado por dia.
3. **Criar pedido** — `POST /api/orders` com itens (preço resolvido no servidor).
4. **Tela web** — lista pedidos (paginação), cria pedidos e mostra o faturamento.

Extras: Swagger documentado, seed automático, gerador de volume sob demanda,
microserviço Node funcional (HTTP) e testes unitários cobrindo toda a camada de
serviço.

---

## Arquitetura

**Clean Architecture** (dependências apontam para dentro) organizada por
**Vertical Slices** (cada caso de uso — Orders, Reports, Catalog — tem seus próprios
DTOs, interfaces e serviço).

```
┌─────────────────────────────────────────────────────────┐
│  Pedidos.Api          Controllers, Swagger, middleware    │  ← detalhes/entrega
├─────────────────────────────────────────────────────────┤
│  Pedidos.Application  Services, DTOs, Interfaces (portas)  │  ← regras de aplicação
├─────────────────────────────────────────────────────────┤
│  Pedidos.Domain       Entities, invariantes de negócio     │  ← núcleo, sem deps
├─────────────────────────────────────────────────────────┤
│  Pedidos.Infrastructure  EF Core, Dapper, Seed, HTTP       │  ← implementa as portas
└─────────────────────────────────────────────────────────┘
```

Regras que segui à risca (pedidas no enunciado):

- **Service só conversa com o seu próprio repositório e com outros services** — nunca
  com o repositório de outro slice. Ex.: `OrderService` usa `IOrderReadRepository` /
  `IOrderWriteRepository` (do próprio agregado) e a porta `IOrderCreatedNotifier`.
- **Services trafegam apenas DTOs, nunca entidades.** As entidades de domínio ficam
  confinadas em `Domain` + `Infrastructure`; **os repositórios já projetam para DTO**.
  Assim a entidade nunca "vaza" para a camada de aplicação.
- **Injeção de dependência** em tudo, via construtores e `IServiceCollection`
  (`AddApplication()`, `AddInfrastructure()`).
- **`summary` XML** em interfaces, DTOs e endpoints — e o Swagger consome esses XMLs.

### Fluxo de "criar pedido"

```
POST /api/orders
  → OrdersController
    → IOrderService.CreateAsync(CreateOrderDto)
        → IOrderWriteRepository.CreateAsync   (EF Core: valida FKs, monta agregado, transação)
        → IOrderCreatedNotifier.NotifyAsync   (HTTP → microserviço Node, best-effort)
  ← 201 Created + OrderDto
```

A notificação é **best-effort**: se o microserviço estiver fora, o pedido (já salvo)
é retornado normalmente e a falha é apenas logada. Há teste unitário cobrindo isso.

---

## EF Core vs. Dapper — onde e por quê

O enunciado pediu uma escolha justificada. O critério adotado:

> **EF Core + LINQ** para escrita e leituras simples/diretas.
> **Dapper** para leitura pesada: agregações e paginação sobre volume.

| Operação | Tecnologia | Por quê |
|----------|-----------|---------|
| **Criar pedido** (`OrderWriteRepository`) | **EF Core** | Escrita transacional (orders + N order_items), validação de FK (cliente/produto), rastreamento de mudanças e INSERT em lote do `SaveChanges`. É onde o EF é mais seguro e produtivo. |
| **Catálogo** clientes/produtos (`CatalogReadRepository`) | **EF Core + LINQ** | Consultas diretas, sem agregação. `AsNoTracking()` + `.Select()` projeta direto para DTO. LINQ é mais legível que SQL aqui. |
| **Listar pedidos paginado** (`OrderReadRepository`) | **Dapper** | Consulta pesada: pagina **milhares** de pedidos e agrega o total/itens de cada um. SQL afiado com `LATERAL JOIN` pagina os pedidos pelo índice de `created_at` **antes** de agregar os itens — em vez de agregar a tabela inteira a cada página. |
| **Faturamento por dia** (`RevenueRepository`) | **Dapper** | Agregação `GROUP BY` por dia cruzando orders × order_items sobre todo o histórico. Mapeamento sem tracking, controle total do SQL. |

Ambas as tecnologias compartilham a **mesma connection string** e o mesmo banco. O EF
cuida do schema (`EnsureCreated`); o Dapper apenas lê.

---

## Seed automático e geração de volume

**Não é preciso criar banco, tabela ou rodar migration manualmente.** No startup, a
API executa `DataSeeder.EnsureSeededAsync`:

1. `EnsureCreatedAsync()` — cria o banco e todas as tabelas se não existirem.
2. Se **não houver pedidos**, popula 2.000 clientes, 300 produtos e 5.000 pedidos.
3. **Idempotente** — se já houver dados, não faz nada (e loga isso).

Há **retry de readiness** (espera o Postgres subir), essencial no docker-compose.

### Gerar muito volume (teste de performance)

Os pedidos/itens são inseridos via **`COPY` binário do Npgsql** — ordens de magnitude
mais rápido que INSERTs um a um. No ambiente de teste: **5.000 pedidos + 17.558 itens
em ~1,1 segundo**.

Para gerar mais massa sob demanda:

```bash
# Gera 100.000 pedidos adicionais (com itens), distribuídos nos últimos 365 dias
curl -X POST "http://localhost:5005/api/seed/orders?count=100000"
```

A massa usa **`Bogus`** com seed fixo (reprodutível) e datas espalhadas em 365 dias,
para o relatório de faturamento por período fazer sentido.

Parâmetros (constantes em `DataSeeder`): `CustomerCount=2000`, `ProductCount=300`,
`CopyBatchSize=10000`. O endpoint de seed aceita `count` de 1 a 1.000.000.

---

## O microserviço Node

`microservices/pedidos-notification` — Express. Recebe o evento de pedido criado,
**enriquece** (classifica por faixa de valor: baixo/médio/alto), simula o envio de uma
notificação e mantém métricas em memória (`GET /stats`).

### Como o .NET conversa com ele (implementado)

**HTTP síncrono best-effort.** Ao criar um pedido, `OrderService` chama
`IOrderCreatedNotifier`, cuja implementação (`HttpOrderCreatedNotifier`, na
Infrastructure) faz `POST /events/order-created` via `IHttpClientFactory` (cliente
nomeado, timeout de 5s). Se falhar, o pedido **não** é desfeito — a falha é logada.

Escolhi HTTP por ser simples de demonstrar e fácil de avaliar (o `docker compose`
sobe os dois lados e o fluxo funciona de imediato). A porta (`IOrderCreatedNotifier`)
vive na camada de Application, então o transporte é um detalhe substituível.

### Como eu desenharia em produção (fila / outbox)

HTTP síncrono acopla a latência e a disponibilidade dos dois serviços. Em produção:

1. **Outbox pattern** — na mesma transação que grava o pedido, grava um registro em
   `outbox_messages`. Um worker lê a outbox e publica numa fila
   (RabbitMQ/SQS/Kafka). Garante *at-least-once* sem perder eventos se o broker cair.
2. **Fila** — o .NET publica `OrderCreated` num exchange; o microserviço Node consome.
   Desacopla totalmente: o pedido é aceito mesmo com o consumidor offline, e o
   processamento vira assíncrono e escalável (múltiplos consumidores).
3. **Idempotência** — o consumidor deduplica por `orderId` (a entrega é at-least-once).

Trocar HTTP por fila exigiria **apenas uma nova implementação de
`IOrderCreatedNotifier`** (ex.: `RabbitMqOrderCreatedNotifier`) — o resto do código
não muda. É o benefício de ter modelado isso como uma porta.

---

## Endpoints

| Método | Rota | Descrição |
|--------|------|-----------|
| `GET` | `/api/orders?page=&pageSize=` | Lista pedidos paginados (com total). |
| `GET` | `/api/orders/{id}` | Pedido completo com itens. |
| `POST` | `/api/orders` | Cria pedido (`{ customerId, items:[{ productId, quantity }] }`). |
| `GET` | `/api/reports/revenue?from=&to=` | Faturamento por dia no período. |
| `GET` | `/api/catalog/customers?take=` | Clientes (para o seletor da UI). |
| `GET` | `/api/catalog/products?take=` | Produtos (para o seletor da UI). |
| `POST` | `/api/seed/orders?count=` | Gera N pedidos (teste de performance). |

Documentação interativa completa no **Swagger**: http://localhost:5005/swagger

---

## Princípios SOLID na prática

- **S** — cada serviço tem uma responsabilidade (Orders, Reports, Catalog); o
  `DataSeeder` só popula; o `HttpOrderCreatedNotifier` só notifica.
- **O** — trocar HTTP por fila = nova implementação de `IOrderCreatedNotifier`, sem
  tocar no `OrderService`. Trocar Postgres = nova `IDbConnectionFactory`.
- **L** — qualquer implementação das interfaces de repositório é substituível; os
  testes injetam mocks no lugar das implementações reais sem efeito colateral.
- **I** — interfaces segregadas: leitura (`IOrderReadRepository`) separada de escrita
  (`IOrderWriteRepository`); `ICatalogService` não carrega métodos de pedido.
- **D** — a Application depende de abstrações (interfaces que ela mesma define); a
  Infrastructure as implementa. As setas de dependência apontam para o núcleo.

---

## Testes

```bash
cd backend && dotnet test
```

17 testes unitários (xUnit + Moq + FluentAssertions) cobrindo **toda a camada de
serviço**: `OrderService`, `ReportService`, `CatalogService` — normalização de
paginação, delegação aos repositórios, disparo da notificação, comportamento
best-effort quando a notificação falha, validação de intervalo de datas e limites.

---

## Estrutura de pastas

```
Pedidos/
├── backend/
│   ├── Pedidos.sln
│   ├── Pedidos.Domain/          Entidades + invariantes (sem dependências)
│   ├── Pedidos.Application/     Services, DTOs, Interfaces (slices: Orders, Reports, Catalog, Notifications)
│   ├── Pedidos.Infrastructure/  EF Core, Dapper, Seed (COPY), HttpNotifier
│   ├── Pedidos.Api/             Controllers, Swagger, middleware de exceção
│   ├── Pedidos.Tests/           Testes unitários da camada de service
│   └── Dockerfile
├── frontend/pedidos-web/        React + Vite + TypeScript + Zustand
├── microservices/
│   └── pedidos-notification/    Express (Node)
├── docs/
│   └── DECISOES.md              Decisões de projeto, com o "porquê" de cada uma
├── docker-compose.yml
├── README.md
└── AI_NOTES.md
```

> O `.sln` ficou em `backend/` (todos os projetos .NET estão lá), e não na raiz, para
> manter o backend autocontido. É só `cd backend` antes de qualquer comando `dotnet`.

---

## Documentos relacionados

- [`AI_NOTES.md`](AI_NOTES.md) — onde a IA ajudou, onde errou e como percebi; inclui o uso do **BMAD Method** (framework agêntico instalado para dar à IA papéis, limites e skills explícitos).
- [`docs/DECISOES.md`](docs/DECISOES.md) — tudo o que foi feito e o porquê de cada escolha.
