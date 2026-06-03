# ServiceRequest Module — Real-Time (SignalR) Testing Guide

## Overview

The ServiceRequest Hub provides real-time notifications to all participants of a service request using SignalR WebSockets.

**Hub URL (Local):** `ws://localhost:7107/hubs/service-request`  
**Hub URL (HTTPS):** `https://localhost:7107/hubs/service-request`  
**Protocol:** SignalR (WebSocket or Long Polling fallback)

---

## Connection Setup

### Authentication
Pass the Keycloak Bearer token via query string or Authorization header:

**Option A: Query String (recommended for WebSocket)**
```
ws://localhost:7107/hubs/service-request?access_token=eyJhbGc...
```

**Option B: Authorization Header**
```
Authorization: Bearer {{active_access_token}}
```

### JavaScript Client Setup

Install the SignalR client:
```bash
npm install @microsoft/signalr
```

Connect to the hub:
```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:7107/hubs/service-request", {
    accessTokenFactory: () => "your_access_token_here"
  })
  .withAutomaticReconnect()
  .configureLogging(signalR.LogLevel.Information)
  .build();

connection.start()
  .then(() => console.log("Connected to ServiceRequest Hub"))
  .catch(err => console.error("Connection error:", err));
```

---

## Subscription Groups

### Subscribe to a Specific Service Request
```javascript
// Join group: servicerequest:{serviceRequestId}
await connection.invoke("SubscribeToServiceRequest", "your-service-request-id");
```

### Unsubscribe from a Service Request
```javascript
await connection.invoke("UnsubscribeFromServiceRequest", "your-service-request-id");
```

### Subscribe as a Provider
```javascript
// Join group: provider:{providerProfileId}
await connection.invoke("SubscribeAsProvider", "your-provider-profile-id");
```

### Subscribe to Admin Operations (Admin Only)
```javascript
// Join group: admin:operations
await connection.invoke("SubscribeToAdminOperations");
```

---

## Receiving Events

### Listen for Messages
```javascript
connection.on("ReceiveMessage", (event) => {
  console.log("Event received:", event);
  console.log("Event Type:", event.eventType);
  console.log("Service Request:", event.serviceRequestId);
  console.log("Payload:", JSON.parse(event.payloadJson));
});
```

### Event Payload Structure (ServiceRequestRealtimeEventDto)
```typescript
interface ServiceRequestRealtimeEventDto {
  serviceRequestId: string;    // GUID
  requestCode: string;         // e.g., "SR-2025-001"
  eventType: number;           // See event types below
  payloadType: string;         // Type name of the payload
  payloadJson: string;         // JSON-serialized payload
  occurredAt: string;          // ISO 8601 datetime
  actorUserId: string;         // User who triggered the event
  actorType: string;           // "Owner" | "Provider" | "Admin" | "System"
}
```

---

## Event Types Reference

| EventType | Value | Triggered By | Payload |
|---|---|---|---|
| ServiceRequestCreated | 1 | POST /service-requests | ServiceRequest details |
| ServiceRequestUpdated | 2 | PUT /service-requests/{id} | Updated fields |
| ServiceRequestStatusChanged | 3 | Publish/Cancel/etc | Old status, new status |
| OfferCreated | 10 | POST /offers | Offer details |
| OfferUpdated | 11 | PUT /offers/{id} | Updated offer |
| OfferAccepted | 12 | PATCH /offers/{id}/accept | Accepted offer |
| OfferRejected | 13 | PATCH /offers/{id}/reject | Rejected offer + reason |
| AssignmentCreated | 20 | POST /assignment | Assignment details |
| AssignmentUpdated | 21 | PUT /assignment/{id} | Updated assignment |
| AssignmentAccepted | 22 | PATCH /assignment/{id}/accept | Assignment accepted |
| AssignmentRejected | 23 | PATCH /assignment/{id}/reject | Assignment rejected + reason |
| MessageSent | 30 | POST /messages | Message content, sender |
| WorkLogAdded | 40 | POST /work-logs/... | Work log details |
| WorkStarted | 41 | PATCH /assignment/{id}/start | Start time |
| WorkPaused | 42 | PATCH /assignment/{id}/pause | Pause time |
| WorkResumed | 43 | PATCH /assignment/{id}/resume | Resume time |
| CompletionSubmitted | 50 | POST /completion/{aId} | Completion details |
| CompletionApproved | 51 | PATCH /completion/approve | Approval notes |
| CompletionRejected | 52 | PATCH /completion/reject | Rejection reason |
| DisputeOpened | 60 | POST /dispute | Dispute details |
| DisputeStatusChanged | 61 | PATCH /dispute/{id}/status | Status change |
| DisputeResolved | 62 | PATCH /dispute/{id}/resolve | Resolution details |
| AdminInterventionRequired | 70 | System | Context details |

---

## Events Expected After Each Scenario Action

| Action | Expected Event | Received By |
|---|---|---|
| Create Service Request | ServiceRequestCreated (1) | Owner group, admin:operations |
| Publish | ServiceRequestStatusChanged (3) | servicerequest:{id}, admin:operations |
| Create Offer | OfferCreated (10) | servicerequest:{id}, owner group |
| Accept Offer | OfferAccepted (12), StatusChanged (3) | servicerequest:{id}, provider:{profileId} |
| Create Assignment | AssignmentCreated (20) | servicerequest:{id}, provider:{profileId} |
| Accept Assignment | AssignmentAccepted (22) | servicerequest:{id}, owner group |
| Start Work | WorkStarted (41), StatusChanged (3) | servicerequest:{id}, owner group |
| Send Message | MessageSent (30) | servicerequest:{id} (all participants) |
| Add Work Log | WorkLogAdded (40) | servicerequest:{id} |
| Submit Completion | CompletionSubmitted (50), StatusChanged (3) | servicerequest:{id}, owner group |
| Approve Completion | CompletionApproved (51), StatusChanged (3) | servicerequest:{id}, provider:{profileId} |
| Open Dispute | DisputeOpened (60) | servicerequest:{id}, admin:operations |
| Resolve Dispute | DisputeResolved (62), StatusChanged (3) | servicerequest:{id}, all participants |

---

## Minimal HTML Test Client

Save as `signalr-test.html` and open in browser to test the hub manually:

```html
<!DOCTYPE html>
<html>
<head>
  <title>ServiceRequest Hub Test</title>
  <script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js"></script>
</head>
<body>
  <h2>ServiceRequest SignalR Hub Tester</h2>
  
  <div>
    <label>Access Token:</label>
    <input type="text" id="tokenInput" style="width:500px" placeholder="Paste Bearer token here" />
  </div>
  <div>
    <label>Service Request ID:</label>
    <input type="text" id="srIdInput" placeholder="GUID" />
  </div>
  <br/>
  <button onclick="connect()">Connect</button>
  <button onclick="subscribe()">Subscribe to SR</button>
  <button onclick="subscribeAdmin()">Subscribe Admin</button>
  <button onclick="disconnect()">Disconnect</button>
  
  <h3>Events:</h3>
  <div id="events" style="border:1px solid #ccc; height:400px; overflow-y:auto; padding:10px; font-family:monospace; font-size:12px;"></div>

  <script>
    let connection = null;
    
    function log(msg) {
      const div = document.getElementById('events');
      const timestamp = new Date().toISOString();
      div.innerHTML = `<div><strong>[${timestamp}]</strong> ${msg}</div>` + div.innerHTML;
    }
    
    async function connect() {
      const token = document.getElementById('tokenInput').value;
      if (!token) { alert('Please enter access token'); return; }
      
      connection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:7107/hubs/service-request', {
          accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();
      
      connection.on('ReceiveMessage', (event) => {
        log(`EVENT ${event.eventType} | SR: ${event.serviceRequestId} | ${event.payloadType} | Occurred: ${event.occurredAt}`);
        log(`&nbsp;&nbsp;Payload: ${event.payloadJson}`);
      });
      
      connection.onclose(() => log('Connection closed'));
      connection.onreconnecting(() => log('Reconnecting...'));
      connection.onreconnected(() => log('Reconnected!'));
      
      try {
        await connection.start();
        log('Connected! Connection ID: ' + connection.connectionId);
      } catch(err) {
        log('ERROR: ' + err.message);
      }
    }
    
    async function subscribe() {
      const srId = document.getElementById('srIdInput').value;
      if (!srId) { alert('Enter Service Request ID'); return; }
      await connection.invoke('SubscribeToServiceRequest', srId);
      log('Subscribed to servicerequest:' + srId);
    }
    
    async function subscribeAdmin() {
      await connection.invoke('SubscribeToAdminOperations');
      log('Subscribed to admin:operations');
    }
    
    async function disconnect() {
      if (connection) await connection.stop();
      log('Disconnected');
    }
  </script>
</body>
</html>
```

---

## Postman WebSocket Testing

Postman supports WebSocket connections for basic testing:

1. Open Postman
2. Click **New** → **WebSocket Request**
3. Enter URL: `ws://localhost:7107/hubs/service-request?access_token={{active_access_token}}`
4. Click **Connect**

**Note:** Postman WebSocket doesn't support the full SignalR protocol (binary framing for method invocation). Use it for:
- Verifying the connection accepts your token
- Observing raw messages

For full Hub method invocation (SubscribeToServiceRequest, etc.), use the HTML test client above or a Node.js script.

### Node.js Test Script
```javascript
const signalR = require("@microsoft/signalr");

const token = "YOUR_ACCESS_TOKEN";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:7107/hubs/service-request", {
    accessTokenFactory: () => token
  })
  .build();

connection.on("ReceiveMessage", (event) => {
  console.log("Event:", JSON.stringify(event, null, 2));
});

connection.start()
  .then(async () => {
    console.log("Connected");
    const srId = "YOUR_SERVICE_REQUEST_ID";
    await connection.invoke("SubscribeToServiceRequest", srId);
    console.log(`Subscribed to servicerequest:${srId}`);
  })
  .catch(err => console.error("Error:", err));
```

---

## Troubleshooting

| Issue | Possible Cause | Fix |
|---|---|---|
| Connection refused | Hub not running | Verify port 7107 and hub registration |
| 401 Unauthorized | Invalid/expired token | Get fresh Keycloak token |
| No events received | Not subscribed to correct group | Call SubscribeToServiceRequest with correct ID |
| Events received without subscription | No group filtering in dev mode | Check hub group configuration |
| CORS error in browser | Hub CORS not configured | Check SignalR CORS policy for localhost |
