# The web app, deployed: the SPA built once with the deployed Keycloak's authority, served by nginx, which forwards /api
# to the host as the Vite dev server does locally (web/app/vite.config.ts). azd builds it with the repository root as
# the context; Dockerfile.dockerignore keeps the context to web/ and this directory.

FROM node:22-alpine AS build
WORKDIR /web
RUN corepack enable
COPY web/package.json web/pnpm-lock.yaml web/pnpm-workspace.yaml ./
COPY web/app/package.json app/
RUN pnpm install --frozen-lockfile
COPY web/app/ app/
# The OIDC authority is read at build time (import.meta.env.VITE_OIDC_AUTHORITY); azd passes the deployed realm's.
ARG VITE_OIDC_AUTHORITY
RUN test -n "$VITE_OIDC_AUTHORITY" || { echo "VITE_OIDC_AUTHORITY is required: the deployed realm's URL." >&2; exit 1; }
RUN VITE_OIDC_AUTHORITY="$VITE_OIDC_AUTHORITY" pnpm --dir app build

FROM nginxinc/nginx-unprivileged:1.29-alpine
# On start the image fills the template's ${HOST_URL} from the container's environment (the host's internal address,
# set by infra/resources.bicep); nginx's own $variables are not in the environment and stay as they are.
COPY --from=build /web/app/dist /usr/share/nginx/html
COPY infra/web/nginx.conf.template /etc/nginx/templates/default.conf.template
EXPOSE 8080
