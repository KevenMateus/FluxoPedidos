# AI_NOTES

Este projeto foi construído com forte uso de IA (principalmente Claude), como é esperado no contexto deste desafio. A proposta nunca foi esconder esse uso, mas demonstrar como ferramentas de IA podem ser utilizadas de forma produtiva, segura e previsível dentro de um processo real de engenharia de software.

Abaixo, descrevo com honestidade onde a IA ajudou, onde ela falhou, quais decisões tomei manualmente e como estruturei o repositório para que tanto pessoas quanto ferramentas de IA consigam trabalhar nele com eficiência.

---

## Como estruturei o projeto para a IA ser produtiva

Este é, para mim, o ponto central do desafio. Foi uma decisão consciente, não um resultado acidental.

### 1. Vertical Slices com fronteiras explícitas

Cada caso de uso (`Orders`, `Reports`, `Catalog`, `Notifications`) possui sua própria estrutura contendo DTOs, contratos, serviços e implementações relacionadas.

Isso reduz drasticamente o contexto necessário para entender uma funcionalidade. Uma pessoa — ou uma IA — consegue abrir apenas um slice e compreender o fluxo completo daquele caso de uso sem precisar navegar pelo sistema inteiro.

O resultado é um menor "raio de explosão" para mudanças e uma evolução mais segura.

---

### 2. Interfaces como contratos legíveis

Contratos como:

* `IOrderReadRepository`
* `IOrderWriteRepository`
* `IOrderCreatedNotifier`

foram tratados como documentação viva.

Cada interface possui XML `summary` explicando:

* o que faz;
* por que existe;
* quando deve ser utilizada;
* quais decisões arquiteturais justificam sua existência.

A IA não precisa inferir intenção apenas pela implementação. O contexto está explícito.

---

### 3. XML Summary em tudo que importa

Os comentários XML cumprem dois papéis:

* documentam a API via Swagger/OpenAPI;
* fornecem contexto local e denso para ferramentas de IA.

Esse investimento reduz ambiguidades e melhora significativamente a qualidade das sugestões geradas.

---

### 4. Separação rígida de camadas

Foi adotada a regra:

> Serviços falam apenas com DTOs.

Entidades não vazam para camadas superiores.

Com isso:

* Infrastructure conhece entidades;
* Application conhece DTOs;
* Controllers conhecem contratos.

Essa restrição estrutural reduz a chance de a IA costurar camadas indevidamente.

---

### 5. Registro explícito das decisões

Além do README, o projeto possui documentação dedicada (`docs/DECISOES.md`) contendo o racional das escolhas mais importantes.

A decisão registrada elimina retrabalho futuro.

A próxima pessoa — ou a próxima sessão de IA — não precisa rediscutir decisões já tomadas.

---

### 6. Convenções previsíveis

O projeto adota convenções consistentes:

* código em inglês;
* textos de negócio em português;
* XML `summary` obrigatório;
* nomenclatura padronizada;
* DTOs como fronteiras.

Previsibilidade reduz alucinação.

---

## Ferramentas e contexto de IA: BMAD Method

Para tornar o trabalho com IA mais previsível, utilizei o BMAD Method (Breakthrough Method of Agile AI-Driven Development).

Trata-se de um framework agêntico baseado em personas especializadas, como:

* Analista;
* Product Manager;
* Arquiteto;
* Scrum Master;
* Desenvolvedor;
* QA.

Cada agente possui:

* responsabilidades claras;
* limites explícitos;
* habilidades específicas.

### Benefícios observados

#### Limites claros

Cada agente possui escopo definido, reduzindo extrapolações indevidas.

#### Skills explícitas

A IA sabe quando deve planejar, implementar ou revisar, evitando misturar responsabilidades.

#### Contexto estruturado

O método incentiva registrar decisões e intenções, complementando perfeitamente a estratégia adotada neste repositório.

---

## O que nasceu manualmente

Apesar do uso intenso de IA, o projeto não começou com um prompt do tipo "faça tudo".

As fundações foram construídas manualmente.

### Estrutura inicial

Foram criados manualmente:

* a Solution;
* os projetos;
* as referências entre projetos;
* a organização inicial das pastas;
* a definição dos Vertical Slices.

---

### Escolha e instalação de dependências

A seleção das tecnologias foi feita conscientemente.

Foram avaliadas e instaladas manualmente bibliotecas como:

* Entity Framework Core;
* Dapper;
* Bogus;
* xUnit;
* Moq;
* Swagger/OpenAPI;
* Docker;
* Docker Compose.

A IA não decidiu quais ferramentas utilizar.

---

### Configuração do ambiente

Também foram feitas manualmente:

* configurações do Docker;
* criação do Compose;
* integração inicial com banco;
* configuração do Swagger;
* referências entre projetos;
* ajustes de build.

---

### Convenções do projeto

Defini pessoalmente regras como:

* nomes em inglês;
* textos de negócio em PT-BR;
* summaries obrigatórios;
* services trabalhando apenas com DTOs;
* documentação das decisões arquiteturais.

A IA operou dentro dessas restrições.

---

## Onde a IA ajudou bastante

A IA foi extremamente útil em tarefas repetitivas e mecânicas.

### Boilerplate

Geração de:

* DTOs;
* Dependency Injection;
* controllers;
* serviços;
* Fluent Configurations;
* arquivos de configuração.

---

### SQL inicial

A primeira versão das consultas Dapper foi produzida pela IA, incluindo:

* paginação;
* agregações;
* estratégias iniciais de JOIN.

Posteriormente revisei, validei e refinei essas abordagens.

---

### Frontend

Acelerou significativamente a construção de:

* stores Zustand;
* componentes React;
* CSS;
* tema escuro.

---

### Testes

A estrutura dos testes foi gerada rapidamente:

* Theory;
* InlineData;
* mocks;
* cenários base.

Os comportamentos cobertos foram revisados manualmente.

---

## Onde a IA errou e como percebi

### 1. COUNT(*) retorna bigint

O Dapper esperava `int`, mas o PostgreSQL retornava `Int64`.

Resultado:

HTTP 400 durante execução real.

Correção:

* `COUNT(*)::int`.

Lição:

> Nem todo erro aparece na compilação.

---

### 2. Faker.Commerce.Price e cultura

O Bogus utilizava cultura invariante.

Em pt-BR:

`123.45`

poderia ser interpretado incorretamente.

Correção:

* `CultureInfo.InvariantCulture`.

---

### 3. Faltou using Xunit

A IA assumiu implicit usings.

Resultado:

dezenas de erros de build.

Correção:

* adicionar `using Xunit`.

---

### 4. Reflection para FK

A primeira implementação utilizava reflection para definir `OrderId`.

Funcionava, mas era frágil.

Correção:

* método interno `AttachToOrder`.

---

### 5. Problemas com buildx

As imagens eram construídas, mas não carregadas pelo runtime.

Correção:

* rebuild com:

`BUILDX_NO_DEFAULT_ATTESTATIONS=1`

---

## O que decidi escrever e decidir à mão

As principais decisões arquiteturais foram humanas.

### Fronteira EF vs Dapper

Essa foi uma decisão minha, tomada antes de qualquer código, e a IA apenas implementou dentro dela.

A regra:

* **EF Core** para **escrita** e leituras **simples e diretas** (criar pedido, mudar status, anexar evento, carregar catálogo). É onde o EF brilha: transação, rastreamento de mudanças, validação de FKs e produtividade.
* **Dapper** para leitura **pesada** (listagem paginada com agregação de totais e relatórios de faturamento com `GROUP BY`). É onde eu quero controle fino do SQL e mapeamento direto, sem o overhead do tracking.

O ponto importante: as duas tecnologias convivem sobre o **mesmo banco e a mesma connection string**. O EF cuida do schema (`EnsureCreated`); o Dapper só lê. Não há duplicação de responsabilidade — cada um atua onde é melhor.

---

### Por que Dapper nas leituras pesadas (e não EF)

Listar milhares de pedidos com o total de cada um, ou somar faturamento por dia sobre todo o histórico, são consultas analíticas. Com EF/LINQ eu teria:

* SQL gerado que eu não controlo (e que tende a materializar mais do que o necessário);
* o custo do change tracking em cima de um resultado que é só leitura.

Com Dapper eu escrevo exatamente o SQL que quero, mapeio direto para `record` imutável e não pago tracking nenhum. Para esse tipo de carga, é a escolha certa.

---

### Estratégia de paginação: paginar pelo índice ANTES de agregar

Essa é a decisão de Dapper da qual mais me orgulho, e foi deliberada.

O caminho ingênuo seria: juntar `orders` com `order_items`, agregar tudo e paginar no fim. Isso obriga o banco a agregar a tabela inteira **a cada página** — não escala.

O que fiz:

1. primeiro seleciono **a página de pedidos** usando o índice de `created_at` (`ORDER BY created_at DESC ... LIMIT/OFFSET`);
2. só então, com `LEFT JOIN LATERAL`, agrego os itens **apenas dos pedidos daquela página**.

Resultado: o trabalho de agregação é proporcional ao tamanho da página, não ao tamanho da tabela. É a diferença entre escalar e não escalar com o volume de seed.

---

### SQL montado com StringBuilder / AppendLine

Optei por construir as consultas Dapper com `StringBuilder` (`Append`/`AppendLine`), em vez de strings literais soltas. Os motivos:

* **Filtros dinâmicos.** A listagem aceita status, busca por cliente e intervalo de datas — todos opcionais. As cláusulas `WHERE` e o `JOIN` de busca são adicionados condicionalmente; montar isso linha a linha fica legível e sem concatenação frágil.
* **Legibilidade e manutenção.** Cada cláusula SQL em sua própria linha deixa a consulta fácil de ler na revisão e fácil de evoluir.
* **Consistência.** Apliquei o mesmo padrão também nas consultas estáticas (detalhe, faturamento, dashboard), para o estilo do acesso a dados ser uniforme.

---

### Segurança e correção nas consultas

Decisões que tomei para as consultas Dapper serem seguras e corretas:

* **Parâmetros, nunca concatenação de valores.** Uso `DynamicParameters` para todo input do usuário (status, busca, datas) — imune a SQL injection mesmo com SQL montado à mão.
* **Busca case-insensitive com `ILIKE`** e `%termo%`, em vez de forçar `LOWER()` dos dois lados.
* **Intervalo de datas half-open** `[from 00:00, (to+1) 00:00)` em **UTC**, para incluir o dia final inteiro sem depender de precisão de hora.
* **`COUNT(*)::int`** — o `COUNT` do Postgres é `bigint`; sem o cast, o mapeamento para os `record` (que usam `int`) quebrava em runtime. Foi um bug real que eu corrigi (detalhado acima).
* **Rótulos resolvidos no domínio.** O SQL traz o `status`/`payment_method` como inteiro; a tradução para PT-BR fica em `Order.StatusLabel`/`PaymentLabel`, não no banco — uma única fonte de verdade.

---

### Notificações como porta

A notificação foi modelada como:

`IOrderCreatedNotifier`

permitindo trocar:

* HTTP hoje;
* fila amanhã.

Sem impacto na aplicação.

---

### Seed orientado à performance

Escolhi utilizar:

* COPY binário.

Em vez de:

* SaveChanges em lote.

O requisito exigia volume suficiente para tornar performance relevante.

---

### Services trabalhando apenas com DTOs

Os repositórios projetam diretamente para DTOs.

Assim:

* entidades permanecem confinadas;
* a regra deixa de ser convenção e passa a ser estrutural.

---

## Fluxo de trabalho adotado

O processo real foi:

1. Definir abordagem e restrições;
2. Solicitar implementação inicial;
3. Revisar criticamente;
4. Ajustar o design;
5. Executar o sistema;
6. Validar comportamento;
7. Corrigir falhas;
8. Documentar decisões.

Em diversos momentos descartei completamente sugestões geradas pela IA.

---

## Reflexão final

A principal lição deste desafio foi perceber que produtividade com IA não significa abrir mão da engenharia.

Quanto mais claras são:

* as fronteiras;
* as convenções;
* as decisões;
* os contratos;
* os critérios de qualidade;

melhor a IA performa.

Este projeto não dependeu de "prompts mágicos".

Ele dependeu de alguém capaz de:

* decompor problemas;
* definir arquitetura;
* estabelecer limites;
* revisar criticamente código gerado;
* validar hipóteses executando o sistema;
* assumir responsabilidade pelas decisões finais.

Em resumo:

> A IA acelerou a implementação.
>
> A engenharia continuou sendo humana.