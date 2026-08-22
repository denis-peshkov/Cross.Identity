# Refresh Token

The lifetime of a **refresh token** depends directly on the security architecture, but there are clear practical guidelines used in production:

## Practical formula
| Mode                       | Access Token | Refresh Token        | Example behavior                                                                                    |
|----------------------------|--------------|----------------------|-----------------------------------------------------------------------------------------------------|
| Web apps (SPA + API)       | 10 - 15 min  | 7–30 days            | User rarely logs out, but not forever. Allows extending the session without logging in again.       |
| Fintech/banks              | 7 - 10 min   | 1–14 days            | Stricter security requirements.                                                                     |
| Standard login             | 15 min       | 7 days               | user is automatically logged out after a week                                                         |
| Remember me                | 30 min       | 60 days              | no need to log in again for 2 months                                                                |
| Service client             | 5 min        | 1 day                | secure API integrator                                                                               |
| Admin panel / bank         | 5 min        | 1 day / no refresh   | heightened security                                                                                 |

## Good practice — refresh token rotation
- On each use of a refresh token:
  - a new **access_token** and **new refresh_token** are issued,
  - the old refresh is immediately **revoked** in the database.
- This prevents reuse (repeated use of a stolen refresh token).

Example configuration in your context (Identity/JWT):
```cs
_accessTokenExpiration = TimeSpan.FromMinutes(15);
_refreshTokenExpiration = TimeSpan.FromDays(30);
```

## Additional security mechanisms
- Check RefreshTokenEntity.ExpiresAt < UtcNow.
- Add a RevokedAt field (if the token is manually revoked).
- Bind the refresh token to:
  - a specific device,
  - IP,
  - user-agent (optional),
  - SecurityStamp (Identity mechanism — reset on password change).

## Security recommendations
| Mechanism           | Why it matters                                                                 |
|---------------------|--------------------------------------------------------------------------------|
| One-time use        | refresh token can be used only once, then it is replaced                       |
| Database storage    | Id, UserId, ExpiresAt, RevokedAt, CreatedAt, CreatedByIp, ReplacedByToken      |
| Device binding      | on login, save device_id or fingerprint                                        |
| Revoke chain        | on compromise of an old token — mark the entire chain as Revoked                |

## Replay detection (family revoke + `REPLAY_DETECTED`)

Rotation alone rejects a reused refresh token. That is **not** enough when the attacker rotated **first**:

1. Attacker steals `R1` and refreshes first → gets active `R2`, `R1` revoked.
2. Victim sends their copy of `R1` → reuse of a revoked refresh.
3. **Without** family revoke: victim gets conflict / must re-login; attacker keeps live `R2`.
4. **With** family revoke (`RefreshTokenRevokedReason.REPLAY_DETECTED`): `R2` and access tokens in the same `FamilyId` are revoked too.

Implemented in `EnsureRefreshTokenActiveForRotationAsync` / `InvalidateRefreshTokenAsync` → `HandleRefreshTokenReplayAsync`.

**Trade-off:** a legitimate retry or concurrent double-refresh can look identical to this race and kill an honest session. That risk is accepted deliberately so the theft race above cannot leave the attacker with a live successor token.

## What happens without rotation
1. You issued the user:
      o	**access_token** — lives, say, 15 minutes;
      o	**refresh_token** — lives, for example, 30 days.
2. After 15 minutes the client calls /token/refresh with the same refresh token.
3. The server issues a new access_token, but **leaves the old refresh token valid**.
4. This refresh can be used **repeatedly** — up to 1000 times, until the 30 days expire.

❗️If it is stolen — an attacker can refresh the token until it expires → **severe security hole**.

## What refresh token rotation does

### On each refresh:
1. The client sends refresh_token_old;
2. The server:
   - verifies that refresh_token_old is still alive and not revoked;
   - **marks it as "used" / "revoked"**;
   - **generates a new refresh_token_new** (new jti, new lifetime);
   - returns a new access_token + refresh_token_new.

👉 The old token becomes invalid immediately after use.

Example in code (flow):
```cs
// refresh_token_step.cs
var oldToken = await _context.RefreshTokens
    .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

if (oldToken is null || oldToken.IsRevoked || oldToken.ExpiresAt < DateTime.UtcNow)
    throw new SecurityException("Invalid or expired refresh token");

// mark old as revoked
oldToken.RevokedAt = DateTime.UtcNow;
oldToken.RevokedIpAddress = request.IpAddress;

// create new
var newRefreshToken = new RefreshTokenEntity
{
    Id = Guid.NewGuid(),
    UserId = oldToken.UserId,
    ExpiresAt = DateTime.UtcNow.AddDays(30),
    CreatedAt = DateTime.UtcNow,
    CreatedByIp = request.IpAddress,
    ReplacedByToken = oldToken.Token
};

_context.RefreshTokens.Add(newRefreshToken);
await _context.SaveChangesAsync();

```

Thus:
- **Access Token** → short-lived (10–30 min), changes frequently.
- **Refresh Token** → lives longer (7–90 days), but **is also renewed** on each refresh so it cannot be reused.


Q: With such rotation, if you open the app every day, THEN the refresh token on the client will always be new and in effect an abstract renewable refresh token will allow logging in forever
A: Absolutely correct observation — you have captured the essence of the entire "eternal refresh token" problem.
That is exactly why **advanced refresh token rotation implementations** use **additional constraints** so the token cannot be used indefinitely, even if the user "logs in every day".

## Basic rotation logic

Yes, if the token is renewed each time and the lifetime is set to "+30 days from the current date", then **continuous client activity** → "eternal" refresh token.

Example:
- refresh lives 30 days,
- user logs in every day → gets a new refresh every day,
- lifetime is "shifted" forward each time → **never expires**.

This scheme is often called **"rolling refresh"**.
It is safe against theft, but does not provide "natural" session expiration.

### What real systems do (Google, Auth0, Okta, Microsoft, etc.)

#### 1. Absolute lifetime (hard session lifetime limit)

Even with refresh token rotation, a maximum lifetime for the token chain is introduced — for example 90 days.

🔸 **"Refresh tokens expire after 90 days regardless of rotation."**
— Azure AD Docs

Implementation:
- add AbsoluteExpiresAt to the RefreshToken table;
- on each rotation:
```cs
if (DateTime.UtcNow > oldToken.AbsoluteExpiresAt)
    throw new SecurityException("Session expired. Please login again.");
```
- the new token gets the same AbsoluteExpiresAt, without shifting.

This guarantees that after, say, 90 days even an active user must log in again.

#### 2. Device binding (session binding)

Store not just a refresh token, but bind the token **family** to metadata captured at login.

Cross.Identity persists host-supplied metadata on `RefreshTokenEntity`:

```cs
public string? CreatedIpAddress { get; set; }
public string? CreatedUserAgent { get; set; }
public string? CreatedDeviceFingerprint { get; set; }
```

On refresh, `EnsureRefreshTokenActiveForRotationAsync` compares the current `ClientContext` with the **family anchor** (values from the first token in `FamilyId`). Mismatch revokes the family with `DEVICE_MISMATCH`, `IP_MISMATCH`, `USER_AGENT_MISMATCH`, or `TOKEN_STOLEN` (two or more dimensions). See `FLOWS.md` — Client context (host).

**Host vs library**

Cross.Identity is a library; it does not read `HttpContext` or the HTTP body. The **host Web API**:

1. Receives the client request (optional `deviceFingerprint` in JSON, headers, SDK token, etc.).
2. Derives trusted metadata (validate or compute fingerprint; `RemoteIpAddress` + `ForwardedHeaders`; request `User-Agent`).
3. Puts them into the flow bag before `ExecuteAsync`:

```csharp
bag["collectForm.IpAddress"] = httpContext.Connection.RemoteIpAddress?.ToString();
bag["collectForm.UserAgent"] = httpContext.Request.Headers.UserAgent.ToString();
bag["collectForm.DeviceFingerprint"] = deviceFingerprintFromHost; // validated / host-computed
```

4. On **login and every refresh** — the same sources. The library reads `ClientContext.Read(bag)` and stores or compares `Created*`.

Do **not** copy `IpAddress` / `UserAgent` blindly from the client JSON into `collectForm` (spoofable). `DeviceFingerprint` should be host-validated (cookie, server session, signed SDK payload), not an arbitrary client string.

**Example API (host)**

The mobile app may send a fingerprint to **your** API:

```http
POST /api/v1/auth/token
{
  "username": "user@example.com",
  "password": "secret",
  "deviceFingerprint": "bdb38b8f2c0a6a17884e23f9a7b05c4e"
}
```

The host handler validates or recomputes that value, then calls Cross.Identity with `collectForm.DeviceFingerprint` set from the **trusted** result — not by forwarding the raw body field into the library unchanged.

(In production, fingerprint libraries such as FingerprintJS are often used on the client; the host still decides what to trust and store.)

Example hash string:

```
"bdb38b8f2c0a6a17884e23f9a7b05c4e"
```

**For mobile (iOS/Android)**

The fingerprint is usually formed as:

```js
device_id = hash(Manufacturer + Model + OSVersion + InstallID)
```

Stored in Secure Storage (Keychain / Keystore). The app sends it to the host API; the host validates and passes it into `ClientContext`.

Good practice — bind up to three dimensions (each optional; only non-empty values are checked):

| Field | Example value | Host source |
|-------|---------------|-------------|
| `DeviceFingerprint` | `bdb38b8f2c0a6a17884e23f9a7b05c4e` | Validated device id / host-computed hash |
| `UserAgent` | `Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)...` | `HttpContext.Request.Headers.User-Agent` |
| `IpAddress` | `203.0.113.42` | `RemoteIpAddress` after `ForwardedHeaders` |
| `IdleTimeout` | sliding window (optional) | Not in stock library |

**IdleTimeout** — optional product feature (e.g. revoke after 7 days without activity); not implemented in stock Cross.Identity.

Example code — absolute expiry (`AbsoluteExpiresAt` is preserved across rotation in `GenerateRefreshTokenAsync`):
```cs
if (oldToken.AbsoluteExpiresAt < DateTime.UtcNow)
{
    oldToken.RevokedAt = DateTime.UtcNow;
    await _context.SaveChangesAsync();
    throw new SecurityException("Session expired. Please log in again.");
}

var newToken = new RefreshTokenEntity
{
    Id = Guid.NewGuid(),
    UserId = oldToken.UserId,
    CreatedAt = DateTime.UtcNow,
    ExpiresAt = DateTime.UtcNow.AddDays(30),
    AbsoluteExpiresAt = oldToken.AbsoluteExpiresAt, // ← do not shift
    ReplacedByToken = oldToken.Token
};
```

## Summary

"Continuous rotation = infinite refresh"
✅ If you do not introduce absolute lifetime, the user can indeed stay logged in forever.
🚫 But production systems always set:
- "rolling" refresh for security,
- absolute lifetime for sessions.
