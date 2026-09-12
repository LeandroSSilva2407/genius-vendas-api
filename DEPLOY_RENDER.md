# Passo a passo — Render

1. Crie um repositório GitHub chamado `genius-vendas-api`.
2. Envie todo o conteúdo desta pasta para a raiz do repositório.
3. No Render, crie **PostgreSQL** e anote o nome do banco.
4. Crie **New > Web Service** e selecione o repositório.
5. Em Language/Runtime escolha **Docker**.
6. Não informe Build Command nem Start Command: o Dockerfile já faz isso.
7. Em Environment adicione `DATABASE_URL` usando a conexão do PostgreSQL do Render.
8. Configure Health Check Path como `/health`.
9. Faça o deploy.
10. Ao aparecer `Live`, abra `https://SEU-SERVICO.onrender.com/health`.
11. Abra os logs do primeiro deploy e copie a `Integration key` gerada.
12. Teste login `antonio / 1234`.

O endereço `*.onrender.com` já é suficiente; domínio próprio é opcional.
