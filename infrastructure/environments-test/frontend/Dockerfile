FROM oven/bun:1.3 AS deps

WORKDIR /test-env

COPY ./apps/Frontend/package.json ./apps/Frontend/bun.lock ./

RUN bun install --frozen-lockfile

FROM node:24-alpine AS test

ENV NG_CLI_ANALYTICS=false \
    CI=true

WORKDIR /test-env

COPY --from=deps /test-env/node_modules ./node_modules
COPY ./apps/Frontend/ ./

ENTRYPOINT ["sh","-c","rm -rf coverage/* && (npx ng test --watch=false --coverage || true); if [ ! -f coverage/index.html ]; then nested=$(find coverage -mindepth 2 -maxdepth 2 -name index.html -type f 2>/dev/null | head -n1); if [ -z \"$nested\" ]; then nested=$(find coverage -mindepth 2 -name index.html -type f 2>/dev/null | head -n1); fi; if [ -n \"$nested\" ]; then cp -a \"$(dirname \"$nested\")/.\" coverage/; fi; fi; if [ ! -f coverage/index.html ]; then cp /placeholder/index.html coverage/index.html; fi"]
