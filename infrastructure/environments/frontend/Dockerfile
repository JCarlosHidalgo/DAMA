FROM oven/bun:1.3 AS builder
WORKDIR /app
ARG API_BASE_URL
COPY ./apps/Frontend/package.json ./apps/Frontend/bun.lock ./
RUN bun install --frozen-lockfile
COPY ./apps/Frontend/ ./
RUN test -n "$API_BASE_URL" || (echo "API_BASE_URL build arg is required" && exit 1)
RUN printf "export const environment = {\n  production: true,\n  apiBaseUrl: '%s',\n};\n" "$API_BASE_URL" > src/environments/environment.ts
# hadolint ignore=DL3059
RUN bun run build

FROM httpd:2.4-alpine
ARG API_BASE_URL
ARG CSP_INLINE_SCRIPT_HASH
COPY ./infrastructure/environments/frontend/httpd.conf /usr/local/apache2/conf/httpd.conf
# hadolint ignore=DL4006
RUN test -n "$API_BASE_URL" || { echo "API_BASE_URL build arg is required"; exit 1; }; \
    test -n "$CSP_INLINE_SCRIPT_HASH" || { echo "CSP_INLINE_SCRIPT_HASH build arg is required"; exit 1; }; \
    API_WS_ORIGIN="$(echo "$API_BASE_URL" | sed -e 's#^https://#wss://#' -e 's#^http://#ws://#')"; \
    sed -i "s#__API_ORIGIN__#${API_BASE_URL}#g; s#__API_WS_ORIGIN__#${API_WS_ORIGIN}#g; s#__CSP_INLINE_SCRIPT_HASH__#${CSP_INLINE_SCRIPT_HASH}#g" /usr/local/apache2/conf/httpd.conf
COPY --from=builder /app/dist/Frontend/browser/ /usr/local/apache2/htdocs/
EXPOSE 80
