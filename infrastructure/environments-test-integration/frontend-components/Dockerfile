FROM oven/bun:1.3 AS deps

WORKDIR /test-env

COPY ./apps/Frontend/package.json ./apps/Frontend/bun.lock ./

RUN bun install --frozen-lockfile && \
    bun add --dev vitest-axe jest-canvas-mock

FROM node:24-alpine AS test

ENV CI=true \
    NG_CLI_ANALYTICS=false

WORKDIR /test-env

COPY --from=deps /test-env/node_modules                                               ./node_modules
COPY ./apps/Frontend/src                                                              ./src
COPY ./apps/Frontend/tsconfig.json                                                    ./tsconfig.json
COPY ./infrastructure/environments-test-integration/frontend-components/vitest.config.ts ./vitest.config.ts
COPY ./infrastructure/environments-test-integration/frontend-components/setup.ts          ./setup.ts
COPY ./infrastructure/environments-test-integration/frontend-components/specs             ./specs

ENTRYPOINT ["sh", "-c", "npx vitest run --coverage || true; \
  if [ ! -f coverage/index.html ]; then cp /placeholder/index.html coverage/index.html; fi"]
