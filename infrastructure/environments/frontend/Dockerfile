FROM oven/bun:1.3 AS builder
WORKDIR /app
ARG API_BASE_URL
COPY ./apps/Frontend/package.json ./apps/Frontend/bun.lock ./
RUN bun install --frozen-lockfile
COPY ./apps/Frontend/ ./
RUN test -n "$API_BASE_URL" || (echo "API_BASE_URL build arg is required" && exit 1)
RUN printf "export const environment = {\n  production: true,\n  apiBaseUrl: '%s',\n};\n" "$API_BASE_URL" > src/environments/environment.ts
RUN bun run build

FROM httpd:2.4-alpine
COPY ./infrastructure/environments/frontend/httpd.conf /usr/local/apache2/conf/httpd.conf
COPY --from=builder /app/dist/Frontend/browser/ /usr/local/apache2/htdocs/
EXPOSE 80
