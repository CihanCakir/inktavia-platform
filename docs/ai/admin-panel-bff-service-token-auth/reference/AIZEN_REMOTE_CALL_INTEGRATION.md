# Aizen RemoteCall Integration

Internal module calls from AdminPanel BFF must use the existing Aizen RemoteCall architecture.

Inspect current repository examples based on:

```csharp
using Aizen.Core.RemoteCall.Abstraction;

public interface ISendMailRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallPost("/token")]
    Task<SendLoginMailResponse> SendLoginRequest([AizenRemoteCallBody] SendLoginMailRequest request);

    [AizenRemoteCallPost("/v1/email/transactional/send")]
    Task<SendLoginMailResponse> SendMailRequest(
        [AizenRemoteCallBody] SendMailRequest request,
        [AizenRemoteCallHeader("Authorization")] string authorization);
}
```

Adapt to actual conventions.

Do not introduce a new Refit client or custom HTTP client if Aizen RemoteCall is the standard.

RemoteCall contracts should receive:

```text
Authorization: Bearer <cached-service-token>
X-Aizen-User-Token: Bearer <incoming-identity-token>
```

Prefer a shared AdminPanel BFF header provider/decorator/service if it avoids repeated token injection code.
