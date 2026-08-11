# tech-challenge-lambda-auth

Function Serverless de autenticação por CPF do Sistema de Gestão de Oficina Mecânica — Tech Challenge SOAT, Fase 3.

## Propósito

Este repositório contém a função serverless que fica atrás do API Gateway e responde pela autenticação:

1. Valida o CPF informado (formato e dígitos verificadores)
2. Consulta a existência e o status do cliente no banco de dados gerenciado
3. Gera e devolve um token JWT válido para consumo das APIs protegidas

Nenhuma outra responsabilidade pertence a este repositório. Regras de negócio de oficina ficam na [aplicação principal](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica).

## Status

> ⚠️ **Scaffold.** O runtime e a linguagem ainda não foram decididos — dependem da RFC de estratégia de autenticação ([issue #35](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/issues/35)). Este README será completado quando a implementação começar.

## Tecnologias

| Item | Definição |
|---|---|
| Runtime | A definir — RFC [#35](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/issues/35) |
| Provedor serverless | A definir — RFC [#56](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/issues/56) |
| Banco consultado | PostgreSQL gerenciado — provisionado em [tech-challenge-infra-database](https://github.com/tech-challenge-grupo-160/tech-challenge-infra-database) |
| Segredos | Gerenciador de segredos da nuvem (chave de assinatura do JWT) |

## Contrato

### Requisição

```
POST /auth
{ "cpf": "12345678909" }
```

### Respostas

| Código | Situação |
|---|---|
| `200` | CPF válido e cliente ativo — retorna o JWT |
| `400` | CPF inválido (formato ou dígito verificador) |
| `403` | Cliente encontrado, porém inativo |
| `404` | Cliente não encontrado |

O formato e as claims do token são definidos na RFC [#35](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/issues/35) e precisam ser aceitos pela API principal.

## Execução local

_A ser documentado junto com a implementação ([issue #36](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/issues/36))._

## Deploy

Deploy automático via GitHub Actions ([issue #50](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/issues/50)), autenticando na nuvem por OIDC — sem chave estática.

| Branch | Ambiente |
|---|---|
| `homolog` | Homologação |
| `main` | Produção |

## Arquitetura

O diagrama de arquitetura na nuvem está em [docs/diagrams](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica/blob/master/docs/diagrams) no repositório principal. Esta função é o componente **Lambda Auth**, em subnet privada.

## Repositórios do projeto

| Repositório | Conteúdo |
|---|---|
| [tech-challenge-oficina-mecanica](https://github.com/tech-challenge-grupo-160/tech-challenge-oficina-mecanica) | API .NET e documentação |
| [tech-challenge-lambda-auth](https://github.com/tech-challenge-grupo-160/tech-challenge-lambda-auth) | Este repositório |
| [tech-challenge-infra-k8s](https://github.com/tech-challenge-grupo-160/tech-challenge-infra-k8s) | Terraform do cluster Kubernetes |
| [tech-challenge-infra-database](https://github.com/tech-challenge-grupo-160/tech-challenge-infra-database) | Terraform do banco gerenciado |

## Contribuição

Branch `main` protegida — sem commits diretos. Toda mudança entra por Pull Request com pelo menos uma aprovação.
