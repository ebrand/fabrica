# Vault Configuration for Fabrica Commerce Cloud
# Production mode with file storage for local development

# Disable memory locking (for Docker)
disable_mlock = true

# Storage backend - file-based for persistence
storage "file" {
  path = "/vault/data"
}

# Listener configuration
listener "tcp" {
  address     = "0.0.0.0:8200"
  tls_disable = true  # Disable TLS for local development
}

# API address
api_addr = "http://0.0.0.0:8200"

# UI enabled
ui = true

# Log level
log_level = "info"
