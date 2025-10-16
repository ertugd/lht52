const express = require('express');
const bodyParser = require('body-parser');

const app = express();
// ChirpStack can send JSON or Protobuf. We'll accept JSON (default common).
app.use(bodyParser.json({ limit: '1mb' }));

// Simple auth token check (set CHIRPSTACK_SECRET in Render env vars)
const SECRET = process.env.CHIRPSTACK_SECRET || '';

app.post('/chirpstack', (req, res) => {
  // Optionally verify a header token:
  const headerToken = req.header('Authorization') || req.header('X-Auth-Token') || '';
  if (SECRET && headerToken !== `Bearer ${SECRET}`) {
    console.warn('Unauthorized request to /chirpstack');
    return res.status(401).send('unauthorized');
  }

  // ChirpStack normally posts a JSON object with metadata and the object/decoded fields.
  // Log for debugging:
  console.log('Got ChirpStack webhook:');
  console.log(JSON.stringify(req.body, null, 2));

  // Extract likely useful fields (adjust based on your payload/template)
  const devEUI = req.body?.devEUI || req.body?.device?.devEUI || req.body?.deviceEUI;
  const fPort = req.body?.fPort || req.body?.frmPayload?.fPort;
  const object = req.body?.object || req.body?.payload || null;

  // Example: store/process/send to DB etc.
  // For demo just return 200 OK.
  res.status(200).send({ status: 'ok' });
});

const port = process.env.PORT || 3000;
app.listen(port, '0.0.0.0', () => console.log(`Server listening on ${port}`));
