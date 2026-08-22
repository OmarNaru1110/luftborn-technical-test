# API Documentation

Base URL (https profile): `https://localhost:7065`
Base URL (http profile): `http://localhost:5129`

All request/response bodies are JSON. Numeric fields use strict JSON number handling
(strings like `"10"` are rejected for numeric fields). An interactive OpenAPI/Swagger UI
is available at the root of the app (`/` in production, `/swagger` route config via
`UseSwaggerUI`, spec at `/openapi/v1.json`).

---

## Songs

### 1. Get all songs

`GET /api/songs`

Returns every song in the catalog.

**Example Request**

```http
GET /api/songs HTTP/1.1
Host: localhost:7065
```

```bash
curl -k https://localhost:7065/api/songs
```

**Response — `200 OK`**

```json
[
  {
    "id": 1,
    "title": "Bohemian Rhapsody",
    "artist": "Queen"
  },
  {
    "id": 2,
    "title": "Stairway to Heaven",
    "artist": "Led Zeppelin"
  }
]
```

---

### 2. Get song by id

`GET /api/songs/{songId}`

| Parameter | Type | In   | Description        |
| --------- | ---- | ---- | ------------------ |
| `songId`  | int  | path | Id of the song     |

**Example Request**

```bash
curl -k https://localhost:7065/api/songs/1
```

**Response — `200 OK`**

```json
{
  "id": 1,
  "title": "Bohemian Rhapsody",
  "artist": "Queen"
}
```

**Response — `404 Not Found`** (song does not exist; body is a JSON string message)

```json
"Song not found."
```

---

## Playlists

### 3. Create playlist

`POST /api/playlists`

| Field    | Type            | Required | Description                                  |
| -------- | --------------- | -------- | -------------------------------------------- |
| `name`   | string          | yes      | Playlist name                                |
| `songIds`| array of int    | no       | Optional songs to include at creation time   |

The playlist is created for the current user (resolved by `ICurrentUser`).

**Example Request**

```http
POST /api/playlists HTTP/1.1
Host: localhost:7065
Content-Type: application/json
```

```json
{
  "name": "Road Trip",
  "songIds": [1, 3, 8]
}
```

```bash
curl -k -X POST https://localhost:7065/api/playlists \
  -H "Content-Type: application/json" \
  -d '{"name":"Road Trip","songIds":[1,3,8]}'
```

**Response — `201 Created`**

```json
{
  "id": 1,
  "name": "Road Trip",
  "userId": 1,
  "createdAt": "2026-08-22T10:15:30.1234567Z",
  "songs": [
    { "id": 1, "title": "Bohemian Rhapsody", "artist": "Queen" },
    { "id": 3, "title": "Hotel California", "artist": "Eagles" },
    { "id": 8, "title": "Billie Jean", "artist": "Michael Jackson" }
  ]
}
```

**Response — `400 Bad Request`** (model validation failure, e.g. missing `name`)
Returns ASP.NET Core validation problem details:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["The Name field is required."]
  }
}
```

---

### 4. Get playlist by id

`GET /api/playlists/{id}`

| Parameter | Type | In   | Description           |
| --------- | ---- | ---- | --------------------- |
| `id`      | int  | path | Id of the playlist    |

**Example Request**

```bash
curl -k https://localhost:7065/api/playlists/1
```

**Response — `200 OK`**

```json
{
  "id": 1,
  "name": "Road Trip",
  "userId": 1,
  "createdAt": "2026-08-22T10:15:30.1234567Z",
  "songs": [
    { "id": 1, "title": "Bohemian Rhapsody", "artist": "Queen" },
    { "id": 3, "title": "Hotel California", "artist": "Eagles" },
    { "id": 8, "title": "Billie Jean", "artist": "Michael Jackson" }
  ]
}
```

**Response — `404 Not Found`**

```json
"Playlist not found."
```

---

### 5. Add songs to playlist

`POST /api/playlists/{id}/songs`

Request body is a JSON array of song ids.

| Parameter | Type         | In   | Description                    |
| --------- | ------------ | ---- | ------------------------------ |
| `id`      | int          | path | Id of the playlist             |
| body      | array of int | body | Song ids to add to playlist    |

Songs already in the playlist are ignored (no duplicates); unknown ids cause an error.

**Example Request**

```http
POST /api/playlists/1/songs HTTP/1.1
Host: localhost:7065
Content-Type: application/json
```

```json
[2, 5]
```

```bash
curl -k -X POST https://localhost:7065/api/playlists/1/songs \
  -H "Content-Type: application/json" \
  -d "[2,5]"
```

**Response — `200 OK`** (updated playlist)

```json
{
  "id": 1,
  "name": "Road Trip",
  "userId": 1,
  "createdAt": "2026-08-22T10:15:30.1234567Z",
  "songs": [
    { "id": 1, "title": "Bohemian Rhapsody", "artist": "Queen" },
    { "id": 2, "title": "Stairway to Heaven", "artist": "Led Zeppelin" },
    { "id": 3, "title": "Hotel California", "artist": "Eagles" },
    { "id": 5, "title": "Smells Like Teen Spirit", "artist": "Nirvana" },
    { "id": 8, "title": "Billie Jean", "artist": "Michael Jackson" }
  ]
}
```

**Response — `404 Not Found`** (playlist not found)

```json
"Song(s) not found."
```

**Response — `400 Bad Request`** (empty/null list)

```json
"No songs provided."
```

---

### 6. Update playlist

`PUT /api/playlists/{id}`

| Field  | Type   | Required | Description       |
| ------ | ------ | -------- | ----------------- |
| `name` | string | yes      | New playlist name |

**Example Request**

```http
PUT /api/playlists/1 HTTP/1.1
Host: localhost:7065
Content-Type: application/json
```

```json
{
  "name": "Summer Road Trip"
}
```

```bash
curl -k -X PUT https://localhost:7065/api/playlists/1 \
  -H "Content-Type: application/json" \
  -d '{"name":"Summer Road Trip"}'
```

**Response — `200 OK`**

```json
{
  "id": 1,
  "name": "Summer Road Trip",
  "userId": 1,
  "createdAt": "2026-08-22T10:15:30.1234567Z",
  "songs": [
    { "id": 1, "title": "Bohemian Rhapsody", "artist": "Queen" },
    { "id": 3, "title": "Hotel California", "artist": "Eagles" },
    { "id": 8, "title": "Billie Jean", "artist": "Michael Jackson" }
  ]
}
```

**Response — `404 Not Found`**

```json
"Playlist not found."
```

---

### 7. Delete playlist

`DELETE /api/playlists/{id}`

| Parameter | Type | In   | Description           |
| --------- | ---- | ---- | --------------------- |
| `id`      | int  | path | Id of the playlist    |

**Example Request**

```bash
curl -k -X DELETE https://localhost:7065/api/playlists/1
```

**Response — `204 No Content`** (empty body)

**Response — `404 Not Found`**

```json
"Playlist not found."
```

---

## Status Code Summary

| Code | Meaning                                                        |
| ---- | -------------------------------------------------------------- |
| 200  | Success (resource returned)                                    |
| 201  | Playlist created                                               |
| 204  | Playlist deleted successfully                                  |
| 400  | Invalid input / failed validation                              |
| 404  | Song or playlist not found                                     |
