import express from 'express';
import cors from 'cors';
import { spawn } from 'child_process';
import path from 'path';
import { fileURLToPath } from 'url';
import fs from 'fs';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const app = express();
const PORT = process.env.PORT || 3800;

// Simple API key for basic security (can be made more robust)
const API_KEY = process.env.SCRIPT_RUNNER_API_KEY || 'fabrica-dev-key';

// CORS configuration - allow requests from Docker containers
app.use(cors({
  origin: '*',
  methods: ['GET', 'POST', 'DELETE', 'PUT'],
  allowedHeaders: ['Content-Type', 'X-API-Key']
}));

app.use(express.json());

// API key validation middleware
const validateApiKey = (req, res, next) => {
  const providedKey = req.headers['x-api-key'];
  if (providedKey !== API_KEY) {
    return res.status(401).json({ error: 'Invalid API key' });
  }
  next();
};

// Find the docker directory
function findDockerDir() {
  // Method 1: Relative to this script
  const relativePath = path.resolve(__dirname, '../../docker');
  if (fs.existsSync(path.join(relativePath, 'fabrica-compose.yml'))) {
    return relativePath;
  }

  // Method 2: Known absolute path
  const absolutePath = '/Users/eric.brand/Documents/source/github/Eric-Brand_swi/fabrica/infrastructure/docker';
  if (fs.existsSync(path.join(absolutePath, 'fabrica-compose.yml'))) {
    return absolutePath;
  }

  return null;
}

const DOCKER_DIR = findDockerDir();
const REDEPLOY_SCRIPT = DOCKER_DIR ? path.join(DOCKER_DIR, 'scripts', 'redeploy.sh') : null;
const KAFKA_TOPIC_SCRIPT = DOCKER_DIR ? path.join(DOCKER_DIR, 'scripts', 'kafka-topic.sh') : null;
const KAFKA_CONSUMER_SCRIPT = DOCKER_DIR ? path.join(DOCKER_DIR, 'scripts', 'kafka-consumer.sh') : null;

// Valid components that can be redeployed
const VALID_COMPONENTS = [
  'mfe-admin', 'shell-admin', 'bff-admin', 'acl-admin',
  'mfe-content', 'bff-content', 'acl-content',
  'mfe-product', 'bff-product', 'acl-product'
];

// Valid Kafka topic actions
const VALID_KAFKA_ACTIONS = ['create', 'delete', 'list', 'describe', 'sync'];

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({
    status: 'ok',
    dockerDir: DOCKER_DIR,
    scripts: {
      redeploy: REDEPLOY_SCRIPT ? fs.existsSync(REDEPLOY_SCRIPT) : false,
      kafkaTopic: KAFKA_TOPIC_SCRIPT ? fs.existsSync(KAFKA_TOPIC_SCRIPT) : false,
      kafkaConsumer: KAFKA_CONSUMER_SCRIPT ? fs.existsSync(KAFKA_CONSUMER_SCRIPT) : false
    },
    validComponents: VALID_COMPONENTS,
    validKafkaActions: VALID_KAFKA_ACTIONS
  });
});

// List available components
app.get('/components', validateApiKey, (req, res) => {
  res.json({
    components: VALID_COMPONENTS,
    dockerDir: DOCKER_DIR
  });
});

// Redeploy a component - uses Server-Sent Events to stream output
app.post('/redeploy/:component', validateApiKey, (req, res) => {
  const { component } = req.params;
  const { noCache = false, killPorts = false } = req.body || {};

  // Validate component
  if (!VALID_COMPONENTS.includes(component)) {
    return res.status(400).json({
      error: `Invalid component: ${component}`,
      validComponents: VALID_COMPONENTS
    });
  }

  // Check if script exists
  if (!REDEPLOY_SCRIPT || !fs.existsSync(REDEPLOY_SCRIPT)) {
    return res.status(500).json({
      error: 'Redeploy script not found',
      path: REDEPLOY_SCRIPT
    });
  }

  // Set up Server-Sent Events
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');
  res.setHeader('X-Accel-Buffering', 'no');

  // Build command arguments
  const args = [component];
  if (noCache) args.push('--no-cache');
  if (killPorts) args.push('--kill-ports');

  console.log(`[${new Date().toISOString()}] Redeploying ${component} with args:`, args);

  // Send initial event
  res.write(`data: ${JSON.stringify({ type: 'start', component, args })}\n\n`);

  // Track if process has completed naturally
  let processCompleted = false;

  // Spawn the script with detached process group
  const child = spawn('bash', [REDEPLOY_SCRIPT, ...args], {
    cwd: DOCKER_DIR,
    env: { ...process.env, FORCE_COLOR: '0', TERM: 'dumb' }, // Disable colors for cleaner output
    stdio: ['ignore', 'pipe', 'pipe'] // Don't inherit stdin
  });

  console.log(`[${new Date().toISOString()}] Spawned process PID: ${child.pid}`);

  // Strip ANSI escape codes for cleaner browser output
  const stripAnsi = (str) => str.replace(/\x1b\[[0-9;]*m/g, '');

  // Stream stdout
  child.stdout.on('data', (data) => {
    const lines = data.toString().split('\n');
    lines.forEach(line => {
      if (line.trim()) {
        try {
          res.write(`data: ${JSON.stringify({ type: 'stdout', line: stripAnsi(line) })}\n\n`);
        } catch (e) {
          // Connection might be closed
        }
      }
    });
  });

  // Stream stderr
  child.stderr.on('data', (data) => {
    const lines = data.toString().split('\n');
    lines.forEach(line => {
      if (line.trim()) {
        try {
          res.write(`data: ${JSON.stringify({ type: 'stderr', line: stripAnsi(line) })}\n\n`);
        } catch (e) {
          // Connection might be closed
        }
      }
    });
  });

  // Handle completion
  child.on('close', (code, signal) => {
    processCompleted = true;
    console.log(`[${new Date().toISOString()}] Redeploy ${component} finished with code ${code}, signal ${signal}`);
    try {
      res.write(`data: ${JSON.stringify({ type: 'complete', code, signal })}\n\n`);
      res.end();
    } catch (e) {
      // Connection might be closed
    }
  });

  // Handle errors
  child.on('error', (error) => {
    processCompleted = true;
    console.error(`[${new Date().toISOString()}] Redeploy ${component} error:`, error);
    try {
      res.write(`data: ${JSON.stringify({ type: 'error', message: error.message })}\n\n`);
      res.end();
    } catch (e) {
      // Connection might be closed
    }
  });

  // Note: We intentionally DON'T kill the process on client disconnect
  // The build should complete regardless of browser state
  req.on('close', () => {
    if (!processCompleted) {
      console.log(`[${new Date().toISOString()}] Client disconnected, but letting process ${child.pid} continue`);
    }
  });
});

// ============================================================
// Kafka Topic Management Endpoints
// ============================================================

// List Kafka topics
app.get('/kafka/topics', validateApiKey, (req, res) => {
  if (!KAFKA_TOPIC_SCRIPT || !fs.existsSync(KAFKA_TOPIC_SCRIPT)) {
    return res.status(500).json({
      error: 'Kafka topic script not found',
      path: KAFKA_TOPIC_SCRIPT
    });
  }

  const child = spawn('bash', [KAFKA_TOPIC_SCRIPT, 'list'], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      const topics = output.split('\n').filter(line => line.trim() && !line.startsWith('Listing'));
      res.json({ topics });
    } else {
      res.status(500).json({ error: errorOutput || 'Failed to list topics', code });
    }
  });
});

// Create a Kafka topic
app.post('/kafka/topics', validateApiKey, (req, res) => {
  const { topicName, partitions = 3 } = req.body;

  if (!topicName) {
    return res.status(400).json({ error: 'topicName is required' });
  }

  // Validate topic name format (domain.table)
  if (!/^[a-z_]+\.[a-z_]+$/.test(topicName)) {
    return res.status(400).json({
      error: 'Invalid topic name format. Expected: domain.table (e.g., admin.user)',
      topicName
    });
  }

  if (!KAFKA_TOPIC_SCRIPT || !fs.existsSync(KAFKA_TOPIC_SCRIPT)) {
    return res.status(500).json({
      error: 'Kafka topic script not found',
      path: KAFKA_TOPIC_SCRIPT
    });
  }

  console.log(`[${new Date().toISOString()}] Creating Kafka topic: ${topicName} with ${partitions} partitions`);

  const child = spawn('bash', [KAFKA_TOPIC_SCRIPT, 'create', topicName, partitions.toString()], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ success: true, message: `Topic '${topicName}' created`, output: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || output || 'Failed to create topic', code });
    }
  });
});

// Delete a Kafka topic
app.delete('/kafka/topics/:topicName(*)', validateApiKey, (req, res) => {
  const topicName = decodeURIComponent(req.params.topicName);

  if (!topicName) {
    return res.status(400).json({ error: 'topicName is required' });
  }

  if (!KAFKA_TOPIC_SCRIPT || !fs.existsSync(KAFKA_TOPIC_SCRIPT)) {
    return res.status(500).json({
      error: 'Kafka topic script not found',
      path: KAFKA_TOPIC_SCRIPT
    });
  }

  console.log(`[${new Date().toISOString()}] Deleting Kafka topic: ${topicName}`);

  const child = spawn('bash', [KAFKA_TOPIC_SCRIPT, 'delete', topicName], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ success: true, message: `Topic '${topicName}' deleted`, output: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || output || 'Failed to delete topic', code });
    }
  });
});

// Describe a Kafka topic
app.get('/kafka/topics/:topicName(*)', validateApiKey, (req, res) => {
  const topicName = decodeURIComponent(req.params.topicName);

  if (!KAFKA_TOPIC_SCRIPT || !fs.existsSync(KAFKA_TOPIC_SCRIPT)) {
    return res.status(500).json({
      error: 'Kafka topic script not found',
      path: KAFKA_TOPIC_SCRIPT
    });
  }

  const child = spawn('bash', [KAFKA_TOPIC_SCRIPT, 'describe', topicName], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ topicName, description: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || 'Failed to describe topic', code });
    }
  });
});

// Sync topics from outbox_config tables
app.post('/kafka/sync', validateApiKey, (req, res) => {
  if (!KAFKA_TOPIC_SCRIPT || !fs.existsSync(KAFKA_TOPIC_SCRIPT)) {
    return res.status(500).json({
      error: 'Kafka topic script not found',
      path: KAFKA_TOPIC_SCRIPT
    });
  }

  console.log(`[${new Date().toISOString()}] Syncing Kafka topics from outbox_config`);

  // Set up Server-Sent Events for streaming output
  res.setHeader('Content-Type', 'text/event-stream');
  res.setHeader('Cache-Control', 'no-cache');
  res.setHeader('Connection', 'keep-alive');

  const child = spawn('bash', [KAFKA_TOPIC_SCRIPT, 'sync'], {
    cwd: DOCKER_DIR
  });

  child.stdout.on('data', (data) => {
    const lines = data.toString().split('\n');
    lines.forEach(line => {
      if (line.trim()) {
        res.write(`data: ${JSON.stringify({ type: 'stdout', line })}\n\n`);
      }
    });
  });

  child.stderr.on('data', (data) => {
    const lines = data.toString().split('\n');
    lines.forEach(line => {
      if (line.trim()) {
        res.write(`data: ${JSON.stringify({ type: 'stderr', line })}\n\n`);
      }
    });
  });

  child.on('close', (code) => {
    res.write(`data: ${JSON.stringify({ type: 'complete', code })}\n\n`);
    res.end();
  });
});

// ============================================================
// Kafka Consumer Group Management Endpoints
// ============================================================

// Helper to check consumer script exists
const checkConsumerScript = (res) => {
  if (!KAFKA_CONSUMER_SCRIPT || !fs.existsSync(KAFKA_CONSUMER_SCRIPT)) {
    res.status(500).json({
      error: 'Kafka consumer script not found',
      path: KAFKA_CONSUMER_SCRIPT
    });
    return false;
  }
  return true;
};

// List consumer groups
app.get('/kafka/consumers', validateApiKey, (req, res) => {
  if (!checkConsumerScript(res)) return;

  const child = spawn('bash', [KAFKA_CONSUMER_SCRIPT, 'list'], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      const groups = output.split('\n').filter(line => line.trim() && !line.startsWith('Listing'));
      res.json({ consumerGroups: groups });
    } else {
      res.status(500).json({ error: errorOutput || 'Failed to list consumer groups', code });
    }
  });
});

// Describe a consumer group
app.get('/kafka/consumers/:groupName', validateApiKey, (req, res) => {
  if (!checkConsumerScript(res)) return;

  const { groupName } = req.params;

  const child = spawn('bash', [KAFKA_CONSUMER_SCRIPT, 'describe', groupName], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ groupName, description: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || 'Failed to describe consumer group', code });
    }
  });
});

// Delete a consumer group
app.delete('/kafka/consumers/:groupName', validateApiKey, (req, res) => {
  if (!checkConsumerScript(res)) return;

  const { groupName } = req.params;

  console.log(`[${new Date().toISOString()}] Deleting consumer group: ${groupName}`);

  const child = spawn('bash', [KAFKA_CONSUMER_SCRIPT, 'delete', groupName], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ success: true, message: `Consumer group '${groupName}' deleted`, output: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || output || 'Failed to delete consumer group', code });
    }
  });
});

// Get consumer group lag
app.get('/kafka/consumers/:groupName/lag', validateApiKey, (req, res) => {
  if (!checkConsumerScript(res)) return;

  const { groupName } = req.params;

  const child = spawn('bash', [KAFKA_CONSUMER_SCRIPT, 'lag', groupName], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ groupName, lag: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || 'Failed to get consumer lag', code });
    }
  });
});

// Get consumer group members
app.get('/kafka/consumers/:groupName/members', validateApiKey, (req, res) => {
  if (!checkConsumerScript(res)) return;

  const { groupName } = req.params;

  const child = spawn('bash', [KAFKA_CONSUMER_SCRIPT, 'members', groupName], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ groupName, members: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || 'Failed to get consumer members', code });
    }
  });
});

// Reset consumer group offsets
app.post('/kafka/consumers/:groupName/reset-offsets', validateApiKey, (req, res) => {
  if (!checkConsumerScript(res)) return;

  const { groupName } = req.params;
  const { topic, resetTo = 'earliest' } = req.body;

  if (!topic) {
    return res.status(400).json({ error: 'topic is required' });
  }

  // Validate resetTo value
  const validResetValues = ['earliest', 'latest'];
  const isOffsetReset = resetTo.startsWith('to-offset:');

  if (!validResetValues.includes(resetTo) && !isOffsetReset) {
    return res.status(400).json({
      error: 'Invalid resetTo value',
      validValues: ['earliest', 'latest', 'to-offset:N']
    });
  }

  console.log(`[${new Date().toISOString()}] Resetting offsets for group '${groupName}' on topic '${topic}' to '${resetTo}'`);

  const child = spawn('bash', [KAFKA_CONSUMER_SCRIPT, 'reset-offsets', groupName, topic, resetTo], {
    cwd: DOCKER_DIR
  });

  let output = '';
  let errorOutput = '';

  child.stdout.on('data', (data) => {
    output += data.toString();
  });

  child.stderr.on('data', (data) => {
    errorOutput += data.toString();
  });

  child.on('close', (code) => {
    if (code === 0) {
      res.json({ success: true, message: `Offsets reset for group '${groupName}'`, output: output.trim() });
    } else {
      res.status(500).json({ error: errorOutput || output || 'Failed to reset offsets', code });
    }
  });
});

// Start server
app.listen(PORT, '0.0.0.0', () => {
  console.log(`
╔════════════════════════════════════════════════════════════════════════╗
║  Fabrica Script Runner Service                                         ║
╠════════════════════════════════════════════════════════════════════════╣
║  Status:     Running                                                   ║
║  Port:       ${PORT}                                                      ║
║  Docker Dir: ${DOCKER_DIR ? 'Found' : 'NOT FOUND'}                                                   ║
║  Scripts:    Redeploy: ${REDEPLOY_SCRIPT && fs.existsSync(REDEPLOY_SCRIPT) ? 'Yes' : 'No '}  Topic: ${KAFKA_TOPIC_SCRIPT && fs.existsSync(KAFKA_TOPIC_SCRIPT) ? 'Yes' : 'No '}  Consumer: ${KAFKA_CONSUMER_SCRIPT && fs.existsSync(KAFKA_CONSUMER_SCRIPT) ? 'Yes' : 'No '}      ║
╠════════════════════════════════════════════════════════════════════════╣
║  Endpoints:                                                            ║
║    GET  /health                       - Health check                   ║
║    GET  /components                   - List valid components          ║
║    POST /redeploy/:component          - Redeploy a component (SSE)     ║
╠════════════════════════════════════════════════════════════════════════╣
║  Kafka Topics:                                                         ║
║    GET  /kafka/topics                 - List all topics                ║
║    POST /kafka/topics                 - Create a topic                 ║
║    GET  /kafka/topics/:name           - Describe a topic               ║
║    DEL  /kafka/topics/:name           - Delete a topic                 ║
║    POST /kafka/sync                   - Sync from outbox_config        ║
╠════════════════════════════════════════════════════════════════════════╣
║  Kafka Consumers:                                                      ║
║    GET  /kafka/consumers              - List consumer groups           ║
║    GET  /kafka/consumers/:group       - Describe a consumer group      ║
║    DEL  /kafka/consumers/:group       - Delete a consumer group        ║
║    GET  /kafka/consumers/:group/lag   - Get consumer lag               ║
║    GET  /kafka/consumers/:group/members - List group members           ║
║    POST /kafka/consumers/:group/reset-offsets - Reset group offsets    ║
╚════════════════════════════════════════════════════════════════════════╝
  `);
});
