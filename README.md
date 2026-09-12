# GeniusVendas.Api — Render + PostgreSQL

API ASP.NET Core .NET 10 preparada para o piloto do Genius Vendas.

## Incluído

- Dockerfile para Render
- PostgreSQL via Npgsql
- `DATABASE_URL` por variável de ambiente
- criação automática das tabelas
- login e token de sessão
- produtos, clientes e pedidos
- cálculo de preço atacado no servidor
- endpoints para o futuro `GeniusVendas.GdoorSync`
- `render.yaml`

## Primeiro deploy no Render

### Opção A — pelo painel

1. Suba esta pasta para um repositório GitHub.
2. Crie o PostgreSQL no Render.
3. Crie um Web Service apontando para o repositório.
4. Runtime: Docker.
5. Vincule a variável `DATABASE_URL` ao banco PostgreSQL criado.
6. Health Check: `/health`.
7. Faça o deploy.

### Opção B — Blueprint

O arquivo `render.yaml` permite criar API e PostgreSQL como Blueprint. Ajuste os planos conforme as opções disponíveis na sua conta.

## Primeiro acesso

Na primeira inicialização, se o banco estiver vazio, a API cria:

- empresa `CLIENTE PILOTO`
- vendedor `ANTONIO`
- usuário `antonio`
- senha `1234`
- uma `integration_key` aleatória

A integration key é mostrada **uma vez nos logs do primeiro startup**. Copie-a; o `GdoorSync` usará:

```http
X-Integration-Key: SUA_CHAVE
```

Troque usuário/senha antes de produção.

## Teste

```text
GET /health
```

Login:

```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "antonio",
  "password": "1234"
}
```

## Endpoints mobile

```text
POST /api/auth/login
GET  /api/products
GET  /api/customers
POST /api/orders
GET  /api/orders/mine
```

## Endpoints GdoorSync

Todos exigem `X-Integration-Key`:

```text
POST /api/sync/products
POST /api/sync/customers
GET  /api/sync/orders/pending
POST /api/sync/orders/{id}/complete
POST /api/sync/orders/{id}/fail
```

## Desenvolvimento local

Tenha PostgreSQL local e use a connection string em `appsettings.Development.json`.

```powershell
dotnet restore
dotnet run
```

## Observação importante

Este projeto **não acessa o Firebird do GDOOR**. O Firebird será acessado pelo terceiro projeto, `GeniusVendas.GdoorSync`, instalado no PC do cliente. O Sync troca dados com esta API por HTTPS.
