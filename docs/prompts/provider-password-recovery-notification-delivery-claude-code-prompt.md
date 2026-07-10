# Claude Code Prompt — Provider Password Recovery: Notification (Email/SMS) OTP Delivery

Close the last production blocker: deliver the recovery OTP through the **Notification module** (email now, SMS
optional) instead of the logging stub. Identity already owns recovery state + OTP generation; this wires real
delivery via the existing message bus + notification pipeline. Do not change the frontend, the BFF, or the public
contract. Do not weaken any recovery security control.

## Architecture (match existing patterns)
```
Identity ProviderPasswordRecoveryDomainService
  → IProviderPasswordRecoveryNotifier.SendOtpAsync(...)                         (Identity)
    → IAizenMessagePublisher.Publish(ProviderPasswordRecoveryOtpRequestedMessage)  (internal RabbitMQ)
      → ProviderPasswordRecoveryOtpRequestedConsumer  (Notification)
        → SendNotificationCommand { Type=PasswordRecoveryOtp, Channel=Email }   (template-driven)
          → INotificationDispatcher → IEmailSender (new) → SMTP/email provider
```
Confirmed conventions: `IAizenMessagePublisher` (Core.Messagebus.Abstraction/Senders), messages implement
`IAizenMessage` and live in a module's `*.Abstraction/Message`, consumers derive
`AizenBaseMessageConsumer<TMessage>`, notifications go through `SendNotificationCommand` (resolves an active template
by `NotificationType`+`NotificationChannel`, interpolates variables, persists a `NotificationEntity`, dispatches via
`INotificationDispatcher`). `NotificationChannel` already has `Email=3, Sms=4`. Only `IFcmSender` (push, stubbed)
exists today — **no email/SMS sender**.

## Part 1 — Message contract (Identity.Abstraction)
`Modules/Identity/src/Aizen.Modules.Identity.Abstraction/Message/ProviderPasswordRecoveryOtpRequestedMessage.cs`
implementing `IAizenMessage`:
```
long   RecipientUserId
string Channel              // "email" | "phone"
string MaskedTarget
string Otp                  // raw code — internal bus only; never to BFF/browser
string? Email
string? Phone
int    ExpiresInMinutes
```
The OTP on the internal bus is acceptable (RabbitMQ, not exposed to the BFF/browser). Keep the message transient
(no archival), and see Part 6 for not persisting the code at rest.

## Part 2 — Identity publisher notifier
- Extend `IProviderPasswordRecoveryNotifier.SendOtpAsync` to carry enough context to deliver: add
  `recipientUserId`, `email`, `phone`, `expiresInMinutes` (the domain service already has the `UserEntity` +
  options at request/resend time — pass them through).
- Add `MessageBusProviderPasswordRecoveryNotifier : IProviderPasswordRecoveryNotifier` in
  `Aizen.Modules.Identity.Repository/.../PasswordRecovery/` that publishes the message via `IAizenMessagePublisher`.
- Keep `LoggingProviderPasswordRecoveryNotifier` as a **local-dev fallback**. Select the impl by a config flag
  `PasswordRecovery:DeliveryMode` (`Notification` default, `Logging` for local) — register the chosen one in
  Identity DI. `DevExposeOtp` remains an independent local-only debug aid.
- Update the domain service call sites (Request + Resend) to pass the richer context.

## Part 3 — Notification type + consumer
- Add `NotificationType.PasswordRecoveryOtp` (e.g. `= 410`) to
  `Aizen.Modules.Notification.Abstraction/Enum/NotificationType.cs`.
- Add `ProviderPasswordRecoveryOtpRequestedConsumer : AizenBaseMessageConsumer<ProviderPasswordRecoveryOtpRequestedMessage>`
  under `Aizen.Modules.Notification/Consumers/Identity/` that, on commit, sends via `ISender`:
  ```
  new SendNotificationCommand {
    RecipientUserId = message.RecipientUserId,
    Type = NotificationType.PasswordRecoveryOtp,
    Channel = message.Channel == "phone" ? NotificationChannel.Sms : NotificationChannel.Email,
    Variables = { {"otp", message.Otp}, {"expiresMinutes", message.ExpiresInMinutes.ToString()},
                  {"maskedTarget", message.MaskedTarget} },
    MetadataJson = null
  }
  ```
  Register the consumer with the bus (follow how the ServiceRequest/Payment consumers are registered).

## Part 4 — Email sender (the missing capability)
- `IEmailSender` in `Notification.Application/Services/` (mirror `IFcmSender`):
  `Task<string> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct);`
- `SmtpEmailSender` implementation using `System.Net.Mail.SmtpClient` (or MailKit if already referenced) reading a
  new `EmailOptions` (Host, Port, User, Password, FromAddress, FromName, UseSsl) from config section `Email`.
  Provide a `LoggingEmailSenderStub` fallback (logs intent, no send) used when SMTP is not configured, so non-email
  envs don't fail.
- Wire `IEmailSender` (and, if you add it, `ISmsSender`) into `INotificationDispatcher` so `Channel=Email` routes to
  `IEmailSender` and `Channel=Sms` routes to the SMS sender. SMS provider may be documented as a further gap if no
  provider is available — Email is the required path.

## Part 5 — Templates (seed)
Seed an **active** notification template for `(PasswordRecoveryOtp, Email)` (and `Sms` if used) via the module's
existing template seeding path (`INotificationTemplateRepository` / seed data / migration used for other templates).
Example Email template (interpolated by `ITemplateInterpolator`, `{{otp}}` etc.):
```
Subject: Inktavia — Password reset code
Body:    Your password reset code is {{otp}}. It expires in {{expiresMinutes}} minutes.
         If you did not request this, ignore this email.
```
Do not include the code in the subject. Keep templates neutral (no account-existence leakage).

## Part 6 — Security
- The raw OTP appears only inside Identity + the internal bus message + the delivered email. It never reaches the
  BFF or the browser (unchanged).
- **Do not persist the OTP at rest in the notifications table.** For `PasswordRecoveryOtp`, either (a) store a
  redacted body (e.g. replace the code with `••••••` before `NotificationEntity.Create`), or (b) add a
  "do-not-persist-body" path for this type. Pick one and document it. The email actually sent still contains the code.
- Keep non-enumeration (a failed publish must not change the generic accepted response — it already degrades to a
  logged warning). No SMTP/SMS credentials committed — all via env.

## Part 7 — Config (docker-compose + appsettings, no secrets)
Identity: `PasswordRecovery__DeliveryMode: Notification` (default). Notification service gets:
```
Email__Host: ${SMTP_HOST}
Email__Port: ${SMTP_PORT:-587}
Email__User: ${SMTP_USER}
Email__Password: ${SMTP_PASSWORD}
Email__FromAddress: ${SMTP_FROM:-no-reply@inktavia.com}
Email__FromName: ${SMTP_FROM_NAME:-Inktavia Marine}
Email__UseSsl: ${SMTP_USE_SSL:-true}
```
Add the same keys (blank) to `.env.example` with a note. When SMTP is unset, the `LoggingEmailSenderStub` is used.

## Part 8 — Build + test
```
dotnet build (Identity 5 projects + Notification: Domain/Abstraction/Application/Repository/host + Core.Messagebus if touched)
```
Runtime: with a real (or Mailtrap/Papercut) SMTP configured, run the recovery smoke and confirm the OTP arrives by
**email** (not just logs); the recovery flow (verify → reset → Keycloak) still passes. With SMTP unset, confirm the
stub logs intent and `DevExposeOtp` still lets local devs read the code.

## Part 9 — Docs
Update `docs/provider-password-recovery-identity-backed-refactor-report.md` §7 (Notification: from "gap" to
"implemented — email via Notification; SMS optional") and note the redaction decision + SMTP env vars. Do not mark
production-ready until a real SMTP provider is configured and the email path is smoke-tested; if SMS is still a stub,
say so.

## Guardrails
- No frontend / BFF / public-contract changes. No new EF schema unless template seeding needs a migration.
- OTP never to the BFF/browser; not persisted at rest in notifications. No committed secrets.
- Keep `DevExposeOtp` default false (already env-driven).
