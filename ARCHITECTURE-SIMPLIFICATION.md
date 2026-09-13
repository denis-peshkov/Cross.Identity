# Cross.Identity — упрощение архитектуры

Re-audit ProcessEngine / OTP / notify / templates.

**Дата:** 2026-09-13 · **ветка:** `release/fix-missed-issues` · **сверка с:** canvas 2026-09-06 (proposals A–L)

Связанные: [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md) (Закрыто M66–M75), [`TO-DO.md`](TO-DO.md) (M40, L1, …), [`../Cross.Identity/FLOWS.md`](../Cross.Identity/FLOWS.md).

---

## Вердикт

Canvas / roadmap **ещё актуален**. С 06.09 закрыты **B** и почти **D/E**; главный долг — **единый OTP verify (A)**. Orphan `register*` / `confirm-email*` и EN-only subjects остаются. `NotificationOptions`: тесты ждут Peshkov defaults, в классе initializers нет (**E′**).

| Метрика | Значение |
|---------|----------|
| OTP verify dual (A) | открыт |
| NotificationComposer (B) | **DONE** |
| Template files | 30 (полная матрица en/ru/ro × txt/html) |
| Security notify events | 1 (`password-changed`) |

---

## Статусы A–L

| # | Proposal | Status | Note |
|---|----------|--------|------|
| **A** | Единый OTP verify | **OPEN** | `ValidateCodeAsync` vs `CodeService.VerifyAsync` |
| **B** | `NotificationComposer` + `ISecurityNotifier` | **DONE** | `SendCode` / `ResetPassword` |
| **C** | Notify via `NotificationMessage` | **PARTIAL** | OTP да; FYI — raw channel/address |
| **D** | Templates cleanup | **PARTIAL** | Матрица 30 OK; orphans `register` / `confirm-email` |
| **E** | `Authentication:Notifications` | **PARTIAL** | Options есть; defaults в классе `null` |
| **F** | i18n subjects | **OPEN** | JSON subjects EN-only |
| **G** | Security notify hooks | **OPEN** | Только `password-changed` |
| **H** | Merge Forgot ↔ RequestCode | **OPEN** | Almost-duplicate JSON |
| **I** | Rename `resetPassword` → `setPassword` | **OPEN** | High breaking — отложить |
| **J** | Public/internal surface | **ACCEPTED** | Email = `IUserService` (2.4 «Принято») |
| **K** | Wire `AuditOperation` | **OPEN** | Password/Code/Login* не пишутся |
| **L** | Drop `IHostEnvironment` on SendCode | **OPEN** | Injected, unused |

---

## Что закрылось с 2026-09-06

### Notify stack (DONE)

- `INotificationComposer` + `ISecurityNotifier`
- `HostSuppliedLanguageContext` → SendCode / ResetPassword
- `Authentication:Notifications` (brand placeholders)
- `password-changed` templates + `ChangeAccountEmail` flow / audit

### Templates matrix (DONE / PARTIAL)

- `verify` / `reset` / `register` / `password-changed` / `confirm-email` × `en`/`ru`/`ro` × txt/html = **30** files
- Были missing: `verify.ru.html`, `register.ru.*` — закрыто
- Stock flows используют только: `verify` / `reset` / `password-changed`

Evidence (план): M66–M75 в [`RELEASE-PLAN-2.4.0.md`](RELEASE-PLAN-2.4.0.md).

---

## Живые нестыковки

| Где | Проблема | Risk |
|-----|----------|------|
| `VerifyCodeStep` vs `ValidateCodeAsync` | Address match + no lockout vs user-only + lockout + `EmailVerified` | **High** |
| OTP vs `SecurityNotifier` | `CodeService` (rate / `DeveloperMode`) vs swallow errors, без `DeveloperMode` | Med |
| `register*` / `confirm-email*` | Не в stock JSON — Register шлёт `verify` | Clarity |
| `NotificationOptions` | Tests ждут peshkov defaults; property initializers отсутствуют | **Bug** |
| `FLOWS.md` | Count 17 vs 18; `ChangeAccountEmail` не в operations table; M40 Register key | Docs |
| `LanguageCode` | В schema почти всех flows; читается только notify paths | Noise |

### Dual OTP verify (деталь)

| Путь | Поведение |
|------|-----------|
| `VerifyCodeStep` → `ICodeService.VerifyAsync` | user + **address**; без lockout; без `EmailVerified` / sync |
| `TokenStep` → `IUserService.ValidateCodeAsync` | channel через `ResolveOtpTargetAsync`; consume OTP **только по `UserAccountId`**; **с** lockout + mark verified + sync |

Файлы: `ProcessEngine/Steps/VerifyCodeStep.cs`, `TokenStep.cs`, `Services/UserService.cs`, `Services/CodeService.cs`.

---

## Gaps (product)

| Gap | Сейчас | Ожидание |
|-----|--------|----------|
| Security notify | `password-changed` only | Email change, logout-all, unlink, stolen refresh |
| Confirm after `ChangeAccountEmail` | Host клеит RequestCode/Token | Stock confirm / VerifyCode → verified |
| `ChangeAccountPhone` | Нет | Симметрия email |
| Messenger OTP | `ToEmailOrSms` stub | Real senders |
| Audit Password/Code/Login* | Enum only | Record on SetPassword / OTP / login |

---

## Карта flows ↔ OTP / notify

| Flow | OTP | Trusted notify | Language |
|------|-----|----------------|----------|
| Register / RequestCode / ForgotPassword | sendCode | — | yes |
| ResetPassword / ChangePassword | verifyCode (reset only) | password-changed | yes |
| Token | `ValidateCodeAsync` | — | bag only |
| ChangeAccountEmail | — | **нет** | bag only |
| Logout* / External* / Endpoints* | — | **нет** | bag only |

---

## Приоритет сейчас

1. **A** — единый OTP verify (`flags`: lockout, markVerified, address match)
2. **E′** — defaults в `NotificationOptions` (контракт тестов)
3. **G** — security notify hooks через `ISecurityNotifier` + DeliveryTarget
4. **L** + FLOWS drift + **D′** orphans (dead DI / docs / delete or wire `register*`)
5. **F** / **K** / **H** — subjects · audit · merge Forgot/RequestCode

**Не трогать как «открытый долг»:** **B** DONE · **J** accepted · **I** отложить (breaking).

---

## Service boundaries

- Account identity (email/password) → `IUserService` + upsert side-effect — **ок** (accepted в 2.4).
- Delivery preference / resolve → `ICommunicationEndpointService` — **ок**.
- Щель: verification после смены email / Register завязана на Token / `ValidateCodeAsync`, не на `VerifyCodeStep`.
- Щель: dual OTP verify (**A**) — главный архитектурный долг.

---

## Dead / unused (кратко)

| Item | Status |
|------|--------|
| `register.*` | Не в stock flows (Register → `verify`) |
| `confirm-email.*` | Orphan для stock |
| `IHostEnvironment` на `SendCodeStep` | Injected, unused (**L**) |
| `TwoFactorEnabled` | Мёртвое (TO-DO L1) |
| `GetPreferredAsync` | Public; steps не вызывают |

Рабочие stock templates: `verify`, `reset`, `password-changed`.
