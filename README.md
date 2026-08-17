# tech-challenge-lambda-auth

Function Serverless de autenticacao de cliente final do Sistema de Gestao de Oficina Mecanica - Tech Challenge SOAT, Fase 3.

## Proposito

Este repositorio contem a Lambda responsavel por autenticar o cliente final por documento e emitir um JWT para consumo das APIs protegidas da aplicacao principal.

Responsabilidades desta Function:

1. Receber a chamada via API Gateway com Lambda Proxy Integration.
2. Validar CPF ou CNPJ, incluindo digitos verificadores.
3. Consultar o cliente no PostgreSQL pela coluna `Cliente.CpfCnpj`.
4. Validar se o cliente esta com status `Ativo`.
5. Gerar e devolver um JWT com role `Cliente`.

Regras administrativas da oficina continuam na API principal.

## Tecnologias

| Item | Definicao |
|---|---|
| Runtime | .NET 10 |
| Provider serverless | AWS Lambda |
| Entrada HTTP | AWS API Gateway com Lambda Proxy Integration |
| Banco consultado | PostgreSQL / AWS RDS |
| Token | JWT assinado com HMAC SHA-256 |

## Contrato

Endpoint esperado no API Gateway:

```http
POST /api/v1/auth/cliente/login
```

Payload:

```json
{
  "documento": "476.548.668-01"
}
```

Tambem sao aceitos os aliases `cpfCnpj` e `cpf` por compatibilidade.

Resposta de sucesso:

```json
{
  "token": "...",
  "expiraEm": "2026-08-17T23:58:02.8162589Z",
  "nomeUsuario": "Vanessa Luna Duarte",
  "role": "Cliente"
}
```

## Configuracao

Variaveis aceitas:

| Variavel | Descricao |
|---|---|
| `ConnectionStrings__DefaultConnection` ou `DATABASE_CONNECTION_STRING` | Connection string do PostgreSQL |
| `Jwt__Issuer` ou `JWT_ISSUER` | Emissor do token |
| `Jwt__Audience` ou `JWT_AUDIENCE` | Audiencia do token |
| `Jwt__SecretKey` ou `JWT_SECRET_KEY` | Chave de assinatura do token |
| `Jwt__ExpirationMinutes` ou `JWT_EXPIRATION_MINUTES` | Tempo de expiracao em minutos |

## Execucao local

Com o Postgres local em Docker:

```powershell
dotnet lambda-test-tool-10.0 --project-location .\Fiap.TechChallenge.OficinaMecanica.AuthLambda
```

Exemplo de payload para o Mock Lambda Test Tool:

```json
{
  "httpMethod": "POST",
  "body": "{\"documento\":\"476.548.668-01\"}",
  "headers": {
    "Content-Type": "application/json"
  },
  "path": "/api/v1/auth/cliente/login",
  "resource": "/api/v1/auth/cliente/login",
  "isBase64Encoded": false
}
```

## Testes

```powershell
dotnet test .\tests\Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests\Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.csproj
```

Os testes unitarios cobrem `Documento`, `AuthService` e `JwtTokenGenerator`.

O `ClienteRepository` deve ser coberto por teste de integracao, pois consulta PostgreSQL real.

## Repositorios do projeto

| Repositorio | Conteudo |
|---|---|
| [tech-challenge-oficina-mecanica](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica) | API .NET e documentacao |
| [tech-challenge-lambda-auth](https://github.com/tech-challenge-grupo-160/tech-challenge-lambda-auth) | Esta Lambda de autenticacao |
| [tech-challenge-infra-database](https://github.com/tech-challenge-grupo-160/tech-challenge-infra-database) | Infraestrutura do banco gerenciado |
