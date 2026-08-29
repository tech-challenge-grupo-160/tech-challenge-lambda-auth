# tech-challenge-lambda-auth

Function Serverless de autenticacao de cliente final do Sistema de Gestao de Oficina Mecanica - Tech Challenge SOAT, Fase 3.

## Proposito

Este repositorio contem as duas funcoes que sustentam a autenticacao do cliente final: a que **emite** o token e a que o **valida** na borda do API Gateway.

| Funcao | Handler | Papel |
|---|---|---|
| Autenticacao | `Function::FunctionHandler` | Autentica por documento e emite o JWT |
| Authorizer | `AuthorizerFunction::FunctionHandler` | Valida o JWT no API Gateway antes de o trafego chegar ao cluster |

As duas saem do **mesmo projeto e do mesmo artefato** - muda so o handler informado no deploy. E deliberado: ambas leem a mesma chave, do mesmo segredo, pelo mesmo `JwtOptions`. Separar em dois projetos duplicaria a resolucao do segredo, que e justamente onde uma divergencia entre elas passaria despercebida ate virar 401 em producao.

### Funcao de autenticacao

1. Receber a chamada via API Gateway com Lambda Proxy Integration.
2. Validar CPF ou CNPJ, incluindo digitos verificadores.
3. Consultar o cliente no PostgreSQL pela coluna `Cliente.CpfCnpj`.
4. Validar se o cliente esta com status `Ativo`.
5. Gerar e devolver um JWT com role `Cliente`.

Regras administrativas da oficina continuam na API principal.

### Funcao authorizer

Lambda authorizer do API Gateway, no formato de payload 2.0 com resposta simples (`isAuthorized`). A escolha por Lambda authorizer em vez do authorizer JWT nativo esta na [RFC-0002](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/blob/develop/docs/rfcs/0002-autenticacao-por-cpf-e-api-gateway.md): o nativo exige emissor OIDC com JWKS publico e assinatura assimetrica, e o token deste projeto e HS256 com segredo compartilhado.

1. Ler o header `Authorization` do evento, sem depender da caixa do nome.
2. Validar assinatura, issuer, audience, expiracao e algoritmo do token.
3. Conferir se as claims do contrato estao presentes.
4. Responder `isAuthorized` e repassar `sub`, `documento` e `role` ao backend em `$context.authorizer`.

Nao consulta o banco: decide sobre o token, nao sobre o cliente. O preco e a janela de ate 60 minutos em que um cliente desativado ainda passa - a mesma janela ja aceita pela API .NET, que tambem valida o token por conta propria (defesa em profundidade).

O motivo de uma recusa vai para o log, nunca para a resposta: o gateway devolve apenas 401, sem corpo. Dizer a quem apresentou um token invalido se o problema foi a assinatura ou a expiracao ajuda quem esta tentando forjar um.

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
| `Jwt__SecretId` ou `JWT_SECRET_ID` | **Nome** do segredo da chave no Secrets Manager. E o caminho usado na AWS |
| `Jwt__SecretKey` ou `JWT_SECRET_KEY` | Chave de assinatura em claro. Apenas execucao local e testes |
| `Jwt__ExpirationMinutes` ou `JWT_EXPIRATION_MINUTES` | Tempo de expiracao em minutos |
| `DB_SECRET_ID` | Nome do segredo da credencial do banco no Secrets Manager |

A chave de assinatura tem duas origens, nesta ordem: `JWT_SECRET_ID` primeiro, `JWT_SECRET_KEY` depois. Sem nenhuma das duas a inicializacao falha - nao ha valor padrao, para que um deploy que esquecesse a variavel nao passasse a assinar tokens com uma chave publicada em repositorio aberto.

As duas funcoes usam a mesma chave. O authorizer precisa de `JWT_SECRET_ID` (ou `JWT_SECRET_KEY`), `JWT_ISSUER` e `JWT_AUDIENCE`; nao precisa de nenhuma variavel de banco.

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

Payload para exercitar o **authorizer** no Mock Lambda Test Tool, apontando o handler para `AuthorizerFunction::FunctionHandler`. O token vai no header, com o esquema `Bearer`:

```json
{
  "type": "REQUEST",
  "routeKey": "GET /api/v1/ordens-servico",
  "rawPath": "/api/v1/ordens-servico",
  "headers": {
    "authorization": "Bearer <token emitido pela funcao de autenticacao>"
  }
}
```

Resposta esperada para um token valido:

```json
{
  "isAuthorized": true,
  "context": {
    "sub": "1000",
    "documento": "47654866801",
    "role": "Cliente"
  }
}
```

## Testes

```powershell
dotnet test .\tests\Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests\Fiap.TechChallenge.OficinaMecanica.AuthLambda.Tests.csproj
```

Os testes unitarios cobrem `Documento`, `AuthService`, `JwtTokenGenerator`, `JwtTokenValidator` e `AuthorizerFunction`.

Os testes do validador emitem os tokens com o proprio `JwtTokenGenerator` em vez de usar strings fixas. E o ponto deles: gerador e validador sao as duas pontas do mesmo contrato, e uma mudanca em um que quebre o outro tem que aparecer no CI - nao em producao, no primeiro 401.

O `ClienteRepository` deve ser coberto por teste de integracao, pois consulta PostgreSQL real.

## Repositorios do projeto

| Repositorio | Conteudo |
|---|---|
| [tech-challenge-oficina-mecanica](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica) | API .NET e documentacao |
| [tech-challenge-lambda-auth](https://github.com/tech-challenge-grupo-160/tech-challenge-lambda-auth) | Esta Lambda de autenticacao |
| [tech-challenge-infra-k8s](https://github.com/tech-challenge-grupo-160/tech-challenge-infra-k8s) | Terraform da rede, do API Gateway e do cluster |
| [tech-challenge-infra-database](https://github.com/tech-challenge-grupo-160/tech-challenge-infra-database) | Infraestrutura do banco gerenciado |
