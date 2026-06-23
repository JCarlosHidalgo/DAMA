FROM alpine:3.20
# hadolint ignore=DL3018
RUN apk add --no-cache openssl bash
COPY ./infrastructure/environments/tls-init/bootstrap-tls.sh /usr/local/bin/bootstrap-tls.sh
RUN chmod +x /usr/local/bin/bootstrap-tls.sh
ENV TLS_DIR=/tls
ENTRYPOINT ["bash","/usr/local/bin/bootstrap-tls.sh"]
