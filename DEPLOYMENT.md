# Despliegue de TicketsHex.API

## Imagen local

El contexto de compilación debe ser la raíz del repositorio:

```bash
docker build -f TicketsHex.API/Dockerfile -t ticketshex-api:local .
docker run --rm -p 8080:8080 --env-file .env ticketshex-api:local
```

La API expone `GET /health` y escucha en `8080` por defecto. Si Railway
inyecta `PORT`, la aplicación escucha automáticamente en ese puerto.

## Configuración y secretos

Copie `.env.example` a `.env` solo para desarrollo local. Nunca confirme
`.env`, cadenas de conexión ni claves privadas en Git.

Railway requiere estas variables en el servicio:

- `ConnectionStrings__DefaultConnection`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ClientId`
- `Jwt__PrivateKeyBase64`
- `Jwt__PublicKeyBase64`

Las claves se convierten a Base64 con:

```bash
base64 -w 0 jwt-private.pem
base64 -w 0 jwt-public.pem
```

En PowerShell:

```powershell
[Convert]::ToBase64String([IO.File]::ReadAllBytes("jwt-private.pem"))
[Convert]::ToBase64String([IO.File]::ReadAllBytes("jwt-public.pem"))
```

## GitHub Container Registry

`.github/workflows/container-cd.yml` ejecuta las pruebas y, en cada push o
merge a `Util_Prod`, publica:

- `ghcr.io/<owner>/<repository>:latest`
- `ghcr.io/<owner>/<repository>:sha-<commit>`

Los pull requests a `Util_Prod` ejecutan pruebas, pero no publican ni despliegan.
Para usar otra rama, cambie `on.push.branches` y `on.pull_request.branches`.

## Railway desde GitHub Actions

1. Cree un proyecto, ambiente y servicio vacío en Railway.
2. Cree un Project Token del ambiente.
3. En GitHub, cree el environment `production`.
4. Agregue el secret `RAILWAY_TOKEN`.
5. Agregue las variables `RAILWAY_SERVICE` y
   `RAILWAY_DEPLOY_ENABLED=true`.
6. Configure en Railway las variables de aplicación indicadas arriba.

El job `deploy-railway` usa `railway up`, por lo que Railway construye el mismo
Dockerfile del commit que ya superó las pruebas. No habilite simultáneamente el
autodeploy nativo de Railway para la misma rama, porque produciría dos
despliegues por commit.

Como alternativa sin token de CI, conecte el repositorio y la rama directamente
en Railway. Railway detectará `railway.json` y el Dockerfile, y desplegará cada
push por su integración nativa.
