FROM oven/bun:1.3.10-alpine AS deps

WORKDIR /work

COPY ./apps/Frontend/package.json ./
COPY ./apps/Frontend/bun.lock ./

RUN bun install --frozen-lockfile


FROM node:24-alpine

RUN apk add --no-cache graphviz

WORKDIR /work

COPY --from=deps /work/node_modules ./node_modules
COPY ./apps/Frontend/package.json ./
COPY ./apps/Frontend/tsconfig.json ./
COPY ./apps/Frontend/tsconfig.app.json ./
COPY ./apps/Frontend/angular.json ./
COPY ./apps/Frontend/src/ ./src/
COPY ./infrastructure/docs/compodoc/tsconfig.doc.json ./tsconfig.doc.json
COPY ./infrastructure/docs/compodoc/tsconfig.depcruise.json ./tsconfig.depcruise.json
COPY ./infrastructure/docs/compodoc/includes/ ./compodoc-includes/

RUN npm install -g @compodoc/compodoc@1.1.30

CMD sh -c "compodoc -p tsconfig.doc.json -d /work/Documentation --theme material --name 'DAMA Frontend Documentation' --hideGenerator --disableCoverage --includes compodoc-includes \
  && grep -q \"sortMethod: 'directed',\" /work/Documentation/js/tree.js \
  && sed -i \"s|sortMethod: 'directed',|sortMethod: 'directed', levelSeparation: 200, nodeSpacing: 220, treeSpacing: 260,|\" /work/Documentation/js/tree.js \
  && ./node_modules/.bin/depcruise src/app --no-config --include-only '^src/app' --exclude '\\.spec\\.ts\$' --do-not-follow 'node_modules' --collapse '^src/app/[^/]+' --ts-config tsconfig.depcruise.json --output-type dot | dot -T svg > /work/Documentation/additional-documentation/global.svg \
  && ./node_modules/.bin/depcruise src/app/core --no-config --include-only '^src/app/core' --exclude '\\.spec\\.ts\$' --do-not-follow 'node_modules' --ts-config tsconfig.depcruise.json --output-type dot | dot -T svg > /work/Documentation/additional-documentation/core.svg \
  && ./node_modules/.bin/depcruise src/app/shared --no-config --include-only '^src/app/shared' --exclude '\\.spec\\.ts\$' --do-not-follow 'node_modules' --ts-config tsconfig.depcruise.json --output-type dot | dot -T svg > /work/Documentation/additional-documentation/shared.svg \
  && ./node_modules/.bin/depcruise src/app/pages --no-config --include-only '^src/app/pages' --exclude '\\.spec\\.ts\$' --do-not-follow 'node_modules' --ts-config tsconfig.depcruise.json --output-type dot | dot -T svg > /work/Documentation/additional-documentation/pages.svg"
