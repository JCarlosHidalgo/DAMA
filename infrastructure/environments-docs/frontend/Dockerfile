FROM oven/bun:1.3.10-alpine AS deps

WORKDIR /work

COPY ./apps/Frontend/package.json ./
COPY ./apps/Frontend/bun.lock ./

RUN bun install --frozen-lockfile


FROM node:24-alpine

WORKDIR /work

COPY --from=deps /work/node_modules ./node_modules
COPY ./apps/Frontend/package.json ./
COPY ./apps/Frontend/tsconfig.json ./
COPY ./apps/Frontend/tsconfig.app.json ./
COPY ./apps/Frontend/angular.json ./
COPY ./apps/Frontend/src/ ./src/
COPY ./infrastructure/docs/compodoc/tsconfig.doc.json ./tsconfig.doc.json

RUN npm install -g @compodoc/compodoc@1.1.30

CMD sh -c "compodoc -p tsconfig.doc.json -d /work/Documentation --theme material --name 'DAMA Frontend Documentation' --hideGenerator --disableCoverage \
  && grep -q \"sortMethod: 'directed',\" /work/Documentation/js/tree.js \
  && sed -i \"s|sortMethod: 'directed',|sortMethod: 'directed', levelSeparation: 200, nodeSpacing: 220, treeSpacing: 260,|\" /work/Documentation/js/tree.js"
