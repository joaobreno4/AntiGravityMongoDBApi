# AntiGravityMongoDBApi — Ecossistema de Microsserviços sob Gravidade Zero

Este repositório contém o código-fonte de um ecossistema de microsserviços tolerante a falhas físicas e sob anomalia gravitacional, construído para demonstrar resiliência técnica, mensageria assíncrona, orquestração local e automação GitOps.

---

## Arquitetura do Ecossistema (Fluxo de Dados)

O fluxo de captura, mitigação e auditoria de destroços espaciais do DOM funciona conforme o seguinte diagrama:

```text
                                 [ 📡 Estação Cliente (DOM) ]
                                              │
                                              ▼ (POST /colisao)
                                 [ FiltroMassaMiddleware ]
                                    /                 \
                                   /                   \
               (Massa >= 50g)     /                     \  (Massa < 50g)
              [Síncrono]         /                       \ [Assíncrono]
                                ▼                         ▼
                    [ API .NET Core ]             [ FilaAmortecimentoDetritos ]
                        │                                  │
                        ▼ (Injeção de DI)                  ▼ (BulkWrite - 5s/500 itens)
                    [ Driver MongoDB ]             [ MotorPurgaService ]
                        │                                  │
                        ▼ (Cabo de Ancoragem)              │
                    [ MongoDB Atlas ] <────────────────────┘
                        │
                        │ (Event Publish)
                        ▼
                [ RabbitMQ Broker ] ─── fila-colisoes-criticas ───► [ AntiGravityAuditService ]
```

---

## Tecnologias Utilizadas

| Tecnologia | Finalidade no Ecossistema | Camada / Módulo |
| :--- | :--- | :--- |
| **.NET 8** | Runtime e compilador de alto desempenho | API & Auditoria |
| **C#** | Linguagem orientada a objetos com tipagem forte | Lógica de Negócio |
| **MongoDB Atlas** | Cluster de dados NoSQL de órbita estável com schema flexível | Armazenamento Principal |
| **RabbitMQ** | Broker de mensageria assíncrona e persistente | Comunicação Inter-serviço |
| **Docker Compose** | Orquestrador de infraestrutura local de simulação | Infraestrutura Local |
| **GitHub Actions** | Esteira de CI/CD automatizada com push de manifesto | Integração Contínua |
| **ArgoCD** | Ferramenta de GitOps com auto-pruning e self-healing | Implantação Contínua |
| **OpenTelemetry** | Coleta de telemetria customizada (Queue Size, Latency) | Observabilidade |

---

## Padrões de Resiliência Implementados

* **Filtro de Massa (Load Shedding):** Middleware Kestrel que intercepta payloads físicos. Elementos com massa inferior a 50g são ejetados da thread principal e processados de forma assíncrona, protegendo a API contra estouro de CPU e conexões de rede em picos de colisão.
* **Motor de Purga (Bulk Operations):** Um worker em segundo plano (`BackgroundService`) drena o buffer em memória e executa gravações em lote no MongoDB Atlas usando `BulkWriteAsync` a cada 5 segundos ou 500 itens, reduzindo I/O de rede.
* **Durabilidade e Confirmação Manual (RabbitMQ):** Fila declarada como `durable: true`, mensagens como `Persistent` e leitura utilizando confirmação manual (`BasicAck` com `prefetchCount: 1`) para evitar perda de dados durante reinicializações inesperadas ou crashes de órbita do microsserviço de auditoria.
* **Rolling Update Sem Gravidade (Zero Downtime):** Implantação Kubernetes com política `maxUnavailable: 0%` e `maxSurge: 25%`, garantindo que os pods v1 antigos permaneçam prestando serviços até que os novos pods v2 passem nas validações da `readinessProbe`.

---

## Como Executar Localmente

### 1. Inicializando a Infraestrutura
Certifique-se de ter o Docker e o Docker Compose instalados em sua máquina. Execute o comando abaixo na raiz do projeto:

```bash
docker compose up --build -d
```

Isso inicializará a API principal, o microsserviço de auditoria, o MongoDB local e o painel de administração do RabbitMQ (disponível em `http://localhost:15672` com as credenciais padrão `guest:guest`).

### 2. Disparando um Teste de Colisão (Física do DOM)
Envie o payload JSON abaixo simulando uma colisão de um link leve (5 gramas) para testar a filtragem de massa:

#### cURL (Bash):
```bash
curl -X POST http://localhost:8080/api/antigravity/colisao \
     -H "Content-Type: application/json" \
     -d '{"nomeId": "link-termos-uso", "coordenadaX": 120.5, "coordenadaY": 950.0, "velocidadeX": 0.5, "velocidadeY": 1.2, "anguloRotacao": 0.0, "largura": 50.0, "altura": 10.0}'
```

#### PowerShell:
```powershell
$body = @{
    nomeId = "link-termos-uso"
    coordenadaX = 120.5
    coordenadaY = 950.0
    velocidadeX = 0.5
    velocidadeY = 1.2
    anguloRotacao = 0.0
    largura = 50.0
    altura = 10.0
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:8080/api/antigravity/colisao" `
                  -Method POST `
                  -ContentType "application/json" `
                  -Body $body `
                  -UseBasicParsing
```

A resposta deve ser **`202 Accepted`** indicando que o detrito de baixa massa foi desviado com sucesso para a fila de memória do orquestrador local.
