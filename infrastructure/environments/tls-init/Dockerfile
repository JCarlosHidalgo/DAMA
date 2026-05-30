FROM alpine:3.20
RUN apk add --no-cache openssl bash
COPY ./infrastructure/bootstrap-tls.sh /usr/local/bin/bootstrap-tls.sh
RUN chmod +x /usr/local/bin/bootstrap-tls.sh
ENV TLS_DIR=/tls
ENTRYPOINT ["bash","/usr/local/bin/bootstrap-tls.sh"]
