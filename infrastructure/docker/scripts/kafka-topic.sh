#!/bin/bash
# Kafka Topic Management Script for Fabrica Commerce Cloud
# Usage: kafka-topic.sh <action> <topic_name> [partitions]
# Actions: create, delete, list, describe

set -e

ACTION=${1:-"list"}
TOPIC_NAME=${2:-""}
PARTITIONS=${3:-3}
REPLICATION_FACTOR=1

KAFKA_CONTAINER="kafka"
KAFKA_BOOTSTRAP="localhost:9092"
KAFKA_BIN="/opt/kafka/bin"

# Check if Kafka container is running
if ! docker ps --format '{{.Names}}' | grep -q "^${KAFKA_CONTAINER}$"; then
    echo "ERROR: Kafka container '${KAFKA_CONTAINER}' is not running"
    exit 1
fi

case "$ACTION" in
    create)
        if [ -z "$TOPIC_NAME" ]; then
            echo "ERROR: Topic name is required for create action"
            echo "Usage: kafka-topic.sh create <topic_name> [partitions]"
            exit 1
        fi

        echo "Creating topic: ${TOPIC_NAME} with ${PARTITIONS} partitions..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-topics.sh \
            --create \
            --topic "${TOPIC_NAME}" \
            --partitions ${PARTITIONS} \
            --replication-factor ${REPLICATION_FACTOR} \
            --bootstrap-server ${KAFKA_BOOTSTRAP} \
            --if-not-exists

        echo "Topic '${TOPIC_NAME}' created successfully"
        ;;

    delete)
        if [ -z "$TOPIC_NAME" ]; then
            echo "ERROR: Topic name is required for delete action"
            echo "Usage: kafka-topic.sh delete <topic_name>"
            exit 1
        fi

        echo "Deleting topic: ${TOPIC_NAME}..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-topics.sh \
            --delete \
            --topic "${TOPIC_NAME}" \
            --bootstrap-server ${KAFKA_BOOTSTRAP} \
            --if-exists

        echo "Topic '${TOPIC_NAME}' deleted successfully"
        ;;

    list)
        echo "Listing all topics..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-topics.sh \
            --list \
            --bootstrap-server ${KAFKA_BOOTSTRAP}
        ;;

    describe)
        if [ -z "$TOPIC_NAME" ]; then
            echo "Describing all topics..."
            docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-topics.sh \
                --describe \
                --bootstrap-server ${KAFKA_BOOTSTRAP}
        else
            echo "Describing topic: ${TOPIC_NAME}..."
            docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-topics.sh \
                --describe \
                --topic "${TOPIC_NAME}" \
                --bootstrap-server ${KAFKA_BOOTSTRAP}
        fi
        ;;

    sync)
        # Sync topics from outbox_config tables in all domain databases
        echo "Syncing Kafka topics from outbox_config tables..."

        # Get topics from admin database
        echo "Fetching topics from fabrica-admin-db..."
        ADMIN_TOPICS=$(docker exec postgres psql -U fabrica_admin -d fabrica-admin-db -t -c \
            "SELECT topic_name FROM cdc.outbox_config WHERE is_active = true;" 2>/dev/null | tr -d ' ')

        # Get topics from product database
        echo "Fetching topics from fabrica-product-db..."
        PRODUCT_TOPICS=$(docker exec postgres psql -U fabrica_admin -d fabrica-product-db -t -c \
            "SELECT topic_name FROM cdc.outbox_config WHERE is_active = true;" 2>/dev/null | tr -d ' ')

        # Combine and deduplicate
        ALL_TOPICS=$(echo -e "${ADMIN_TOPICS}\n${PRODUCT_TOPICS}" | sort -u | grep -v '^$')

        if [ -z "$ALL_TOPICS" ]; then
            echo "No topics found in outbox_config tables"
            exit 0
        fi

        echo "Topics to sync:"
        echo "$ALL_TOPICS"
        echo ""

        # Create each topic
        for topic in $ALL_TOPICS; do
            echo "Creating topic: ${topic}..."
            docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-topics.sh \
                --create \
                --topic "${topic}" \
                --partitions ${PARTITIONS} \
                --replication-factor ${REPLICATION_FACTOR} \
                --bootstrap-server ${KAFKA_BOOTSTRAP} \
                --if-not-exists 2>/dev/null || true
        done

        echo ""
        echo "Topic sync complete!"
        ;;

    *)
        echo "Unknown action: ${ACTION}"
        echo "Usage: kafka-topic.sh <action> <topic_name> [partitions]"
        echo "Actions:"
        echo "  create <topic> [partitions]  - Create a new topic"
        echo "  delete <topic>               - Delete a topic"
        echo "  list                         - List all topics"
        echo "  describe [topic]             - Describe topic(s)"
        echo "  sync                         - Sync topics from outbox_config tables"
        exit 1
        ;;
esac
