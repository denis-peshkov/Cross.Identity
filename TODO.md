`SecurityStamp` — это сигнал «у аккаунта изменилось что-то критичное для входа». После ротации stamp обычно ещё и гасят сессии (у вас — revoke всех access/refresh).

Сейчас stamp крутится только в двух местах:

- смена/сброс пароля (`SetPasswordAsync`)
- unlink external login (`UnlinkAsync`)

Ниже — события, которых в API ещё нет (или они есть частично), но по Identity-семантике тоже должны крутить stamp.

## Зачем вообще крутить stamp на «других» событиях

Не любой апдейт пользователя. Только когда:

1. меняются **credentials / способы входа**, или
2. аккаунт **больше не должен считаться «тем же trusted session»**, или
3. админ/система **форсирует re-login**.

Иначе старые refresh/access (украденные или с другого устройства) продолжают жить до TTL, даже если «логика аккаунта» уже другая.

У вас revoke токенов делает основную работу; stamp — запасной маркер + будущая опора, если появятся cookie/stateless checks.

---

## 1. Disable account (`ACCOUNT_DISABLED`)

**Сценарий:** пользователь заблокирован (бан, fraud, `IsActive = false`, lockout).

**Почему stamp:** все сессии должны умереть сразу, не ждать expiry access token.

**Что должно произойти:**

1. `IsActive = false` (или `LockoutEnd = …`)
2. `SecurityStamp = NewGuid()`
3. `RevokeAllTokensForUserAsync(..., ACCOUNT_DISABLED)`

**Сейчас:** поля `IsActive` / lockout есть, отдельного «disable + stamp + revoke» API нет. Просто выставить `IsActive=false` в БД **не** убьёт уже выданные токены, пока validate смотрит только `RevokedAt`/`ExpiresAt`, а не `IsActive`/`SecurityStamp`.

---

## 2. Admin revoke (`ADMIN_REVOKE` / `USER_LOGOUT_ALL`)

**Сценарий:** админ жмёт «выйти со всех устройств» / «отозвать сессии», без смены пароля.

**Почему stamp:** это явное «все старые сессии недействительны».

**Что должно произойти:**

1. stamp rotate
2. revoke all tokens (`ADMIN_REVOKE` или `USER_LOGOUT_ALL`)

Пароль можно не трогать.

**Сейчас:** reason в enum есть, flow/endpoint logout-all — нет. Есть только revoke **одного** refresh на logout.

---

## 3. MFA reset (`MFA_RESET`)

**Сценарий:** сброс TOTP/SMS 2FA, отключение 2FA, замена authenticator после потери телефона.

**Почему stamp:** MFA — фактор входа. После сброса старые сессии, открытые «до сброса», часто считают недоверенными (особенно если сброс делали через recovery/support).

**Что должно произойти:**

1. обновить 2FA-настройки
2. stamp rotate
3. revoke all (`MFA_RESET`)

**Сейчас:** `TwoFactorEnabled` на entity есть, MFA flow/reset API нет → и ротации stamp под это нет.

---

## 4. Смена email / phone

**Сценарий:** пользователь меняет email или телефон, которыми логинится / получает OTP.

**Почему stamp (часто да):**

- это смена identifier + recovery channel;
- старый email/phone больше не должен открывать старые «подтверждённые» сессии как будто ничего не было;
- если украли сессию, смена email без kill sessions оставляет атакующего залогиненным.

**Нюанс:** иногда делают soft-path — stamp только после confirm нового адреса. До confirm старый email ещё «главный».

**Типичный строгий вариант:**

1. confirm нового email/phone
2. обновить поле + `EmailConfirmed`/`PhoneConfirmed`
3. stamp rotate
4. revoke all (или хотя бы все кроме текущей сессии — продуктовое решение)

**Сейчас:** confirm флагами через `ValidateCodeAsync` есть, отдельного «change email/phone + stamp» нет. Confirm кода **не** ротирует stamp и **не** ревокает токены (это нормально для first-time verify; для **смены** email — уже слабо).

---

## Сводная таблица

| Событие | Крутить stamp? | Revoke tokens? | Есть в коде сейчас |
|--------|----------------|----------------|--------------------|
| Password change/reset | да | да (`PASSWORD_CHANGED`) | ✅ |
| Unlink external login | да | да (`EXTERNAL_LOGIN_REMOVED`) | ✅ |
| Disable / lock account | да | да (`ACCOUNT_DISABLED`) | ❌ API |
| Admin / logout-all | да | да (`ADMIN_REVOKE` / `USER_LOGOUT_ALL`) | ❌ API |
| MFA reset / disable | да | да (`MFA_RESET`) | ❌ MFA |
| Change email/phone (после confirm) | обычно да | обычно да | ❌ change-flow |
| Обычный profile update (display name) | нет | нет | — |
| Pepper rehash при login | нет | нет | правильно |

---

## Как это стыкуется с «stamp в JWT»

Пока токены **stored + Validate\* смотрит RevokedAt**, revoke all уже гасит сессии. Stamp всё равно полезен как:

- единый сигнал «security event произошёл»;
- защита на будущее (cookie Identity / claim check);
- явная семантика в БД (аудит: stamp изменился ⇒ был security event).

Если завтра access станет stateless без DB — **без stamp-in-claim** disable/password-change не убьёт access до exp.
