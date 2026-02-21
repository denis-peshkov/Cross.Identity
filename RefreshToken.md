# Refresh Token

Срок жизни **refresh token** напрямую зависит от архитектуры безопасности, но есть чёткие практические ориентиры, применяемые в проде:

## Практическая формула
| Режим                      | Access Token | Refresh Token        | Пример поведения                                                                             |
|----------------------------|--------------|----------------------|----------------------------------------------------------------------------------------------|
| Веб-приложения (SPA + API) | 10 - 15 мин  | 7–30 дней            | Пользователь редко выходит из системы, но не навсегда. Позволяет продлить сессию без логина. |
| Финтех/банки               | 7 - 10 мин   | 1–14 дней            | Усиленные требования к безопасности.                                                         |
| Обычный вход               | 15 мин       | 7 дней               | пользователь автоматически выходит через неделю                                              |
| Remember me                | 30 мин       | 60 дней              | можно не входить заново 2 месяца                                                             |
| Сервисный клиент           | 5 мин        | 1 день               | безопасный API-интегратор                                                                    |
| Админ-панель / банк        | 5 мин        | 1 день / без refresh | повышенная безопасность                                                                      |

## Хорошая практика — ротация refresh-токенов
- При каждом использовании refresh-токена:
  - выдается новый **access_token** и **новый refresh_token**,
  - старый refresh немедленно **аннулируется** в БД.
- Это предотвращает reuse (повторное использование украденного refresh-токена).

Пример конфигурации в твоём контексте (Identity/JWT):
```cs
_accessTokenExpiration = TimeSpan.FromMinutes(15);
_refreshTokenExpiration = TimeSpan.FromDays(30);
```

## Безопасные доп-механизмы
- Проверять RefreshTokenEntity.ExpiresAt < UtcNow.
- Добавлять поле RevokedAt (если токен вручную аннулируют).
- Привязывать refresh-токен к:
  - конкретному устройству,
  - IP,
  - user-agent (по желанию),
  - SecurityStamp (Identity-механизм — сбрасывается при смене пароля).

## Рекомендации по безопасности
| Механизм              | Почему важен                                                              |
|-----------------------|---------------------------------------------------------------------------|
| One-time use          | refresh-токен можно использовать только 1 раз, после чего он заменяется   |
| Хранение в БД         | Id, UserId, ExpiresAt, RevokedAt, CreatedAt, CreatedByIp, ReplacedByToken |
| Привязка к устройству | при логине сохраняй device_id или fingerprint                             |
| Revoke chain          | при компрометации старого токена — пометь всю цепочку как Revoked         |

## Что происходит без ротации
1. Ты выдал пользователю:
      o	**access_token** — живёт, скажем, 15 минут;
      o	**refresh_token** — живёт, например, 30 дней.
2. Клиент через 15 минут делает /token/refresh с тем же refresh-токеном.
3. Сервер выдаёт новый access_token, но **оставляет старый refresh-токен действительным**.
4. Этот refresh можно использовать **повторно** — хоть 1000 раз, пока не истечёт 30 дней.

❗️Если его украдут — злоумышленник сможет обновлять токен до конца его жизни → **severe security hole**.

## Что делает ротация refresh-токенов

### При каждом обновлении:
1. Клиент присылает refresh_token_old;
2. Сервер:
   - проверяет, что refresh_token_old ещё жив и не отозван;
   - **помечает его как “использованный” / “revoked”**;
   - **генерирует новый refresh_token_new** (новый jti, новый срок жизни);
   - возвращает новый access_token + refresh_token_new.

👉 Старый токен становится недействительным сразу после использования.

Пример в коде (flow):
```cs
// refresh_token_step.cs
var oldToken = await _context.RefreshTokens
    .FirstOrDefaultAsync(t => t.Token == request.RefreshToken);

if (oldToken is null || oldToken.IsRevoked || oldToken.ExpiresAt < DateTime.UtcNow)
    throw new SecurityException("Invalid or expired refresh token");

// помечаем старый
oldToken.RevokedAt = DateTime.UtcNow;
oldToken.RevokedByIp = request.IpAddress;

// создаём новый
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

Таким образом:
- **Access Token** → короткий (10–30 мин), меняется часто.
- **Refresh Token** → живёт дольше (7–90 дней), но **тоже обновляется** при каждом refresh, чтобы нельзя было его reuse.


Q: В случае такой ротации если заходить в приложение каждый день, ТО рефреш токен будет на клиенте вечно новый и по факту абстрактный обновляемый рефреш токен позволит заходить вечно
A: Абсолютно правильное наблюдение — ты поймал суть всей проблемы “вечных” refresh-токенов.
Именно поэтому **продвинутые реализации ротации refresh-токенов** используют **дополнительные ограничения**, чтобы токен нельзя было использовать бесконечно, даже если пользователь “каждый день заходит”.

## Базовая логика ротации

Да, если токен каждый раз обновляется и срок жизни ставится “+30 дней от текущей даты”, то **постоянная активность клиента** → “вечный” refresh token.

Пример:
- refresh живёт 30 дней,
- пользователь логинится каждый день → каждый день получает новый refresh,
- срок жизни каждый раз “сдвигается” вперёд → **никогда не истечёт**.

Такую схему часто называют **“rolling refresh”**.
Она безопасна от кражи, но не даёт “естественного истечения” сессии.

### Что делают в реальных системах (Google, Auth0, Okta, Microsoft, etc.)

#### 1. Absolute lifetime (жёсткий лимит жизни сессии)

Даже при ротации refresh-токенов, вводится максимальный срок существования цепочки токенов — например 90 дней.

🔸 **“Refresh tokens expire after 90 days regardless of rotation.”**
— Azure AD Docs

Реализация:
- в таблице RefreshToken добавить AbsoluteExpiresAt;
- при каждой ротации:
```cs
if (DateTime.UtcNow > oldToken.AbsoluteExpiresAt)
    throw new SecurityException("Session expired. Please login again.");
```
- новый токен получает тот же AbsoluteExpiresAt, без сдвига.

Это гарантирует, что через, скажем, 90 дней даже активный пользователь должен будет перелогиниться.

#### 2. Device binding

Храним не просто refresh-токен, а привязываем его к конкретному устройству / user-agent / IP.

Например:
```cs
public string DeviceFingerprint { get; set; } = default!;
```

Тогда даже если токен украдут с другого устройства — он не сработает.

(В проде обычно используют библиотеку вроде FingerprintJS, которая делает это надёжнее.)

На выходе ты получаешь строку хеша, например:
```
"bdb38b8f2c0a6a17884e23f9a7b05c4e"
```

При логине клиент отправляет deviceFingerprint:
```http
POST /api/v1/auth/token
{
  "username": "user@example.com",
  "password": "secret",
  "deviceFingerprint": "bdb38b8f2c0a6a17884e23f9a7b05c4e"
}
```

Сервер сохраняет этот DeviceFingerprint в RefreshTokenEntity:
```cs
public class RefreshTokenEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }

    public string DeviceFingerprint { get; set; } = default!;
    public string UserAgent { get; set; } = default!;
    public string? IpAddress { get; set; }
}
```

При refresh-запросе сервер сравнивает:
```cs
if (!string.Equals(oldToken.DeviceFingerprint, request.DeviceFingerprint, StringComparison.Ordinal))
    throw new SecurityException("Device mismatch — refresh token invalid.");
```

Для Mobile (iOS/Android)

Tам fingerprint обычно формируется как:
```js
device_id = hash(Manufacturer + Model + OSVersion + InstallID)
```

И хранится в Secure Storage (Keychain / Keystore).
Хорошая практика — хранить не один, а два поля:

| Поле              | Пример значения                                                                    | Назначение                |
|-------------------|------------------------------------------------------------------------------------|---------------------------|
| DeviceFingerprint | "bdb38b8f2c0a6a17884e23f9a7b05c4e"                                                 | постоянный хеш устройства |
| UserAgent         | "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)..."                               | человекочитаемое описание |
| IdleTimeout       | (скользящее окно) — необязательно (например, 7 дней без активности → инвалидируем) |                           |

Пример кода:
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
    AbsoluteExpiresAt = oldToken.AbsoluteExpiresAt, // ← не сдвигаем
    ReplacedByToken = oldToken.Token
};
```

## Итог

“Постоянная ротация = бесконечный refresh”
✅ Если не ввести absolute lifetime, пользователь действительно сможет быть залогинен вечно.
🚫 Но в production-системах всегда ставят:
- “rolling” refresh для безопасности,
- absolute lifetime для сессий.
