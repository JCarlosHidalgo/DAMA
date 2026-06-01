FROM nginx:1.27.2

ENV NGINX_ENVSUBST_OUTPUT_DIR=/etc/nginx
ENV NGINX_ENVSUBST_FILTER=GATEWAY_FRONTEND_ORIGIN

COPY ./infrastructure/environments/api-gateway/nginx.conf /etc/nginx/templates/nginx.conf.template