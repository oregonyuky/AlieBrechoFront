# Deploy do AlieBrechoFront no Railway

O projeto usa o `Dockerfile` da raiz e lê a porta fornecida pelo Railway automaticamente.

## Serviço

1. Crie um serviço a partir deste repositório.
2. Gere um domínio público em **Settings > Networking**.
3. Configure a variável abaixo, substituindo `api` pelo nome do serviço backend no mesmo projeto Railway:

```text
AlieBrechoApi__BaseUrl=http://${{api.RAILWAY_PRIVATE_DOMAIN}}:${{api.PORT}}/
```

O serviço backend precisa ter uma variável `PORT` explícita (por exemplo, `8080`) para que a referência `${{api.PORT}}` seja resolvida. Defina `PORT=8080` também no front para manter a configuração previsível.

## Persistência recomendada

Anexe um volume em `/app/App_Data` para preservar as chaves de Data Protection entre deploys. Sem o volume, cookies emitidos antes de um novo deploy podem deixar de ser válidos.

Mantenha uma única réplica enquanto carrinho e sessão estiverem no cache em memória desta aplicação.

## Variáveis opcionais

```text
Instagram__InstagramBusinessAccountId=...
Instagram__AccessToken=...
```

O health check `/healthz` já está configurado em `railway.json`.
