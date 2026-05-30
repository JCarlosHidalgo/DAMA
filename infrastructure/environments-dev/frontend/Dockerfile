FROM oven/bun:1.3 AS builder
WORKDIR /app
COPY ./apps/Frontend/package.json ./apps/Frontend/bun.lock ./
RUN bun install --frozen-lockfile
COPY ./apps/Frontend/ ./
RUN bun run build

FROM httpd:2.4-alpine
COPY ./infrastructure/environments-dev/frontend/httpd.conf /usr/local/apache2/conf/httpd.conf
COPY --from=builder /app/dist/Frontend/browser/ /usr/local/apache2/htdocs/
EXPOSE 80
