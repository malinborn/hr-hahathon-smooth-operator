# HTTP API Documentation

## Authentication

All API endpoints (except `/health`) require authentication via the `X-API-Key` header:

```
X-API-Key: <SERVICE_API_KEY>
```

**401 Unauthorized** will be returned if the API key is missing or invalid.

## Endpoints

### Health Check

**GET** `/health`

Check if the service is running.

**Response:**
```json
{
  "status": "healthy"
}
```

---

### Send Answer

**POST** `/answer`

Send a message either to a thread or as a direct message (DM).

#### Thread Mode

Send a reply to a thread by providing `channel_id` and `root_id`.

**Request:**
```json
{
  "channel_id": "string",
  "root_id": "string",
  "text": "string",
  "props": {},
  "file_ids": ["id1", "id2"]
}
```

#### DM Mode

Send a direct message by providing `user_id` or `username`.

**Request:**
```json
{
  "user_id": "string",
  "text": "string"
}
```

Or:

```json
{
  "username": "string",
  "text": "string"
}
```

**Response:**
```json
{
  "ok": true,
  "post_id": "string",
  "channel_id": "string",
  "root_id": "string|null"
}
```

**Error Responses:**
- **400 Bad Request**: Invalid request (missing required fields)
- **401 Unauthorized**: Invalid or missing API key
- **404 Not Found**: User or thread not found
- **502 Bad Gateway**: Mattermost API error
- **500 Internal Server Error**: Unexpected error

---

### Get Thread

**POST** `/get_thread`

Retrieve the root post and replies from a thread.

**Request:**
```json
{
  "root_id": "string",
  "limit": 50,
  "order": "asc"
}
```

**Parameters:**
- `root_id` (required): ID of the root post
- `limit` (optional, default: 50): Maximum number of replies to return
- `order` (optional, default: "asc"): Sort order - "asc" (oldest first) or "desc" (newest first)

**Response:**
```json
{
  "ok": true,
  "root": {
    "id": "string",
    "channel_id": "string",
    "user_id": "string",
    "create_at": 1731240000000,
    "message": "string",
    "root_id": null,
    "props": {},
    "files": []
  },
  "replies": [
    {
      "id": "string",
      "channel_id": "string",
      "user_id": "string",
      "create_at": 1731243600000,
      "message": "string",
      "root_id": "string",
      "props": {},
      "files": []
    }
  ]
}
```

**Error Responses:**
- **400 Bad Request**: Invalid request (missing root_id or invalid order)
- **401 Unauthorized**: Invalid or missing API key
- **404 Not Found**: Thread not found
- **502 Bad Gateway**: Mattermost API error
- **500 Internal Server Error**: Unexpected error

---

## Examples

See `src/MmProxy/api-examples.http` for complete request examples.

### Send Thread Reply

```bash
curl -X POST http://localhost:8080/answer \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "channel_id": "abc123",
    "root_id": "xyz789",
    "text": "This is a reply"
  }'
```

### Get Thread

```bash
curl -X POST http://localhost:8080/get_thread \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "root_id": "xyz789",
    "limit": 10,
    "order": "desc"
  }'
```

### Send DM

```bash
curl -X POST http://localhost:8080/answer \
  -H "Content-Type: application/json" \
  -H "X-API-Key: your-api-key" \
  -d '{
    "username": "john.doe",
    "text": "Hello from the bot!"
  }'
```
