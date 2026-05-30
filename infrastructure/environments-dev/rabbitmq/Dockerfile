# RabbitMQ with the delayed-message-exchange plugin enabled.
# The plugin powers the per-message x-delay header used by Payment's debt-expiration flow.
FROM rabbitmq:4.1-management-alpine

# Must match the broker's major/minor line; bump together with the FROM tag.
ARG DELAYED_PLUGIN_VERSION=4.1.0

RUN apk add --no-cache curl ca-certificates \
    && curl -fSL -o /opt/rabbitmq/plugins/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez \
        "https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v${DELAYED_PLUGIN_VERSION}/rabbitmq_delayed_message_exchange-${DELAYED_PLUGIN_VERSION}.ez" \
    && rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
