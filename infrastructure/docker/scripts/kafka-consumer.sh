#!/bin/bash
# Kafka Consumer Group Management Script for Fabrica Commerce Cloud
# Usage: kafka-consumer.sh <action> [group_name] [options]
# Actions: list, describe, delete, reset-offsets, lag

set -e

ACTION=${1:-"list"}
GROUP_NAME=${2:-""}
TOPIC_NAME=${3:-""}
RESET_TO=${4:-"earliest"}

KAFKA_CONTAINER="kafka"
KAFKA_BOOTSTRAP="localhost:9092"
KAFKA_BIN="/opt/kafka/bin"

# Check if Kafka container is running
if ! docker ps --format '{{.Names}}' | grep -q "^${KAFKA_CONTAINER}$"; then
    echo "ERROR: Kafka container '${KAFKA_CONTAINER}' is not running"
    exit 1
fi

case "$ACTION" in
    list)
        echo "Listing all consumer groups..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
            --list \
            --bootstrap-server ${KAFKA_BOOTSTRAP}
        ;;

    describe)
        if [ -z "$GROUP_NAME" ]; then
            echo "ERROR: Consumer group name is required for describe action"
            echo "Usage: kafka-consumer.sh describe <group_name>"
            exit 1
        fi

        echo "Describing consumer group: ${GROUP_NAME}..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
            --describe \
            --group "${GROUP_NAME}" \
            --bootstrap-server ${KAFKA_BOOTSTRAP}
        ;;

    delete)
        if [ -z "$GROUP_NAME" ]; then
            echo "ERROR: Consumer group name is required for delete action"
            echo "Usage: kafka-consumer.sh delete <group_name>"
            exit 1
        fi

        echo "Deleting consumer group: ${GROUP_NAME}..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
            --delete \
            --group "${GROUP_NAME}" \
            --bootstrap-server ${KAFKA_BOOTSTRAP}

        echo "Consumer group '${GROUP_NAME}' deleted successfully"
        ;;

    reset-offsets)
        if [ -z "$GROUP_NAME" ]; then
            echo "ERROR: Consumer group name is required for reset-offsets action"
            echo "Usage: kafka-consumer.sh reset-offsets <group_name> <topic_name> [earliest|latest|to-offset:N]"
            exit 1
        fi

        if [ -z "$TOPIC_NAME" ]; then
            echo "ERROR: Topic name is required for reset-offsets action"
            echo "Usage: kafka-consumer.sh reset-offsets <group_name> <topic_name> [earliest|latest|to-offset:N]"
            exit 1
        fi

        echo "Resetting offsets for group '${GROUP_NAME}' on topic '${TOPIC_NAME}' to ${RESET_TO}..."

        case "$RESET_TO" in
            earliest)
                docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
                    --reset-offsets \
                    --group "${GROUP_NAME}" \
                    --topic "${TOPIC_NAME}" \
                    --to-earliest \
                    --execute \
                    --bootstrap-server ${KAFKA_BOOTSTRAP}
                ;;
            latest)
                docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
                    --reset-offsets \
                    --group "${GROUP_NAME}" \
                    --topic "${TOPIC_NAME}" \
                    --to-latest \
                    --execute \
                    --bootstrap-server ${KAFKA_BOOTSTRAP}
                ;;
            to-offset:*)
                OFFSET=${RESET_TO#to-offset:}
                docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
                    --reset-offsets \
                    --group "${GROUP_NAME}" \
                    --topic "${TOPIC_NAME}" \
                    --to-offset ${OFFSET} \
                    --execute \
                    --bootstrap-server ${KAFKA_BOOTSTRAP}
                ;;
            *)
                echo "ERROR: Invalid reset-to value: ${RESET_TO}"
                echo "Valid values: earliest, latest, to-offset:N"
                exit 1
                ;;
        esac

        echo "Offsets reset successfully"
        ;;

    lag)
        if [ -z "$GROUP_NAME" ]; then
            echo "Showing lag for all consumer groups..."
            # Get all groups and describe each
            GROUPS=$(docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
                --list \
                --bootstrap-server ${KAFKA_BOOTSTRAP} 2>/dev/null)

            for group in $GROUPS; do
                echo ""
                echo "=== Consumer Group: $group ==="
                docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
                    --describe \
                    --group "$group" \
                    --bootstrap-server ${KAFKA_BOOTSTRAP} 2>/dev/null || true
            done
        else
            echo "Showing lag for consumer group: ${GROUP_NAME}..."
            docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
                --describe \
                --group "${GROUP_NAME}" \
                --bootstrap-server ${KAFKA_BOOTSTRAP}
        fi
        ;;

    members)
        if [ -z "$GROUP_NAME" ]; then
            echo "ERROR: Consumer group name is required for members action"
            echo "Usage: kafka-consumer.sh members <group_name>"
            exit 1
        fi

        echo "Listing members of consumer group: ${GROUP_NAME}..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
            --describe \
            --group "${GROUP_NAME}" \
            --members \
            --bootstrap-server ${KAFKA_BOOTSTRAP}
        ;;

    state)
        if [ -z "$GROUP_NAME" ]; then
            echo "ERROR: Consumer group name is required for state action"
            echo "Usage: kafka-consumer.sh state <group_name>"
            exit 1
        fi

        echo "Showing state of consumer group: ${GROUP_NAME}..."
        docker exec ${KAFKA_CONTAINER} ${KAFKA_BIN}/kafka-consumer-groups.sh \
            --describe \
            --group "${GROUP_NAME}" \
            --state \
            --bootstrap-server ${KAFKA_BOOTSTRAP}
        ;;

    *)
        echo "Unknown action: ${ACTION}"
        echo ""
        echo "Usage: kafka-consumer.sh <action> [group_name] [options]"
        echo ""
        echo "Actions:"
        echo "  list                                    - List all consumer groups"
        echo "  describe <group>                        - Describe a consumer group"
        echo "  delete <group>                          - Delete a consumer group"
        echo "  reset-offsets <group> <topic> [to]      - Reset offsets (earliest|latest|to-offset:N)"
        echo "  lag [group]                             - Show consumer lag"
        echo "  members <group>                         - List group members"
        echo "  state <group>                           - Show group state"
        exit 1
        ;;
esac
