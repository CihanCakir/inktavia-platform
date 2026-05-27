# Aizen.Core.Realtime — Kullanım Kılavuzu ve Command/Query Örnekleri

Bu doküman, oluşturduğumuz domain-odaklı Realtime altyapısını (Aizen.Core.Realtime.Abstraction + Aizen.Core.Realtime) Inktavia-platform modüllerinde nasıl kullanacağınızı, komut/querie örneklerini ve mesaj akışlarını adım adım anlatır. Dosyayı projenizde `Core/Realtime/docs/README-realtime.md` olarak ekleyin.

Özet
- Core: Realtime altyapısı (SignalR publisher, SocketManager, Ingress, HubFilter, RateLimit).
- Abstraction: modeller ve arayüzler (EventDto, RealtimeMessage, IRealtimeEventIngress, IEventSocketMapper vb.).
- Modüller: domain-hub, mapper, authorization servislerini kendileri sağlar.
- Mesaj akışı: Command → (DB commit / CQRS publish) → Commit consumer → IRealtimeEventIngress → IRealtimePublisher → ISocketManager → Client.

İçindekiler
- Hızlı başlatma (Quickstart)
- DI / Startup (Program.cs) örneği (module-side)
- Command handler örneği (domain event üretip realtime'a publish)
- Query örneği (sorgu + isteğe bağlı realtime tetikleme)
- MassTransit (Prepare/Commit/Rollback) consumer örneği (commit aşamasında realtime)
- IEventSocketMapper örneği
- Client (JS) örneği: bağlanma, join, receive
- Akışların zamanlaması: ne zaman ne tetiklenir
- Test & QA kısa rehberi
- Dosya/yer önerileri

---

Hızlı başlatma (özet)
1. Core Realtime servislerini register edin:
```csharp
builder.Services.AddAizenRealtime(builder.Configuration);
```
2. Module içinde hub'ı ve domain mapping'i tanımlayın:
```csharp
builder.Services.AddDomainHub<ModuleActivityHub>("activity");
```
3. Module-specific implementasyonları register edin:
```csharp
builder.Services.AddScoped<IEventSocketMapper, ActivityEventSocketMapper>();
builder.Services.AddScoped<IActivityAuthorizationService, IdentityActivityAuthorizationService>();
builder.Services.AddRealtimeDomainEvents("activity", "activity.created", "activity.updated", "activity.checkin");
```
4. Pipeline'da connection metadata ve auth sırasını uygulayın, hub endpoint'ini module içinde map edin:
```csharp
app.UseConnectionMetadata();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();

endpoints.MapHub<ModuleActivityHub>("/hubs/activity");
```

---

DI / Program.cs (module örneği)
```csharp
// Modules/Identity/src/Program.cs (öz)
var builder = AizenApplicationBuilder.CreateBuilder(...);

// core registrations
builder.Services.AddAizenRealtime(builder.Configuration);

// module-hub mapping
builder.Services.AddDomainHub<ModuleActivityHub>("activity");

// module implementations
builder.Services.AddScoped<IEventSocketMapper, ActivityEventSocketMapper>();
builder.Services.AddScoped<IActivityAuthorizationService, IdentityActivityAuthorizationService>();
builder.Services.AddRealtimeDomainEvents("activity", "activity.created", "activity.updated", "activity.checkin");

var app = builder.Build();
app.UseConnectionMetadata();
app.UseAuthentication();
app.UseAuthorization();
app.UseRouting();

// module maps its hub endpoint
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<ModuleActivityHub>("/hubs/activity");
    endpoints.MapHub<Aizen.Core.Realtime.Hubs.ChatHub>("/hubs/chat");
});
app.Run();
```

---

Command handler örneği (Application katmanı)
- Amaç: bir komut işlendiğinde domain event'i yaratıp realtime'a bildirim göndermek (direct ingress pattern).
- Örnek: Activity oluşturma sonrası bildirim.

Command sınıfı:
```csharp
public class CreateActivityCommand : Aizen.Core.CQRS.Message.AizenCommand<ActivityCreatedDto>
{
    public string Title { get; init; }
    public DateTime StartUtc { get; init; }
    // ... diğer alanlar
}
```

Handler (ingress kullanarak realtime publish):
```csharp
public sealed class CreateActivityCommandHandler : AizenCommandHandler<CreateActivityCommand, ActivityCreatedDto>
{
    private readonly IRealtimeEventIngress _realtimeIngress;
    // ... repo, uow, vb

    public CreateActivityCommandHandler(IRealtimeEventIngress realtimeIngress /*, ... */)
    {
        _realtimeIngress = realtimeIngress;
    }

    public override async Task<ActivityCreatedDto?> Handle(CreateActivityCommand command, CancellationToken ct)
    {
        // 1) Domain işlemleri: create entity, validation, repo.Add, uow.Commit vb.
        var entity = ActivityEntity.Create(...);
        await _activityRepo.AddAsync(entity);

        // commit / save changes...
        // After commit: create an EventDto and publish to realtime
        var evt = new Aizen.Core.Realtime.Abstraction.Models.EventDto
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = entity.Id.ToString(),
            Type = "activity.created",
            Data = new { entity.Id, entity.Title },
            Metadata = new Dictionary<string,string> { ["title"] = entity.Title }
        };

        await _realtimeIngress.PublishDomainEventAsync(evt, ct);

        return new ActivityCreatedDto { Id = entity.Id, Title = entity.Title };
    }
}
```

Notlar:
- Eğer uygulama Outbox pattern kullanıyorsa EventDto outbox'a yazılır, background worker MessageBus'a publish eder; Realtime consumer commit aşamasında dinler.

---

Query örneği (sorgu + opsiyonel realtime)
- Queryler tipik olarak okuma amaçlıdır ve doğrudan realtime tetiklemez. Ancak bazı durumlarda bir Query sonucu kullanıcıya push edilebilir (örn: admin panel'den "force check-in" sonrası durumu push).
- Örnek: Check-in validate query → server hub'a directly Publish (örnek küçük senaryo):

Query:
```csharp
public class ValidateQrQuery : IAizenRequest<CheckInStatusDto>
{
    public string QrCode { get; init; }
}
```

Query handler (örn. validation + realtime bildirimi):
```csharp
public class ValidateQrQueryHandler : IAizenRequestHandler<ValidateQrQuery, CheckInStatusDto>
{
    private readonly IRealtimeEventIngress _ingress;
    public ValidateQrQueryHandler(IRealtimeEventIngress ingress) => _ingress = ingress;

    public async Task<CheckInStatusDto> Handle(ValidateQrQuery request, CancellationToken ct)
    {
        var result = await _qrService.ValidateAsync(request.QrCode, ct);
        var dto = new CheckInStatusDto { ActivityId = result.ActivityId, ParticipantId = result.ParticipantId, Result = result.Success ? CheckInResult.Success : CheckInResult.InvalidQr };

        // Optionally push to activity organizer channel
        var evt = new Aizen.Core.Realtime.Abstraction.Models.EventDto
        {
            Id = Guid.NewGuid().ToString(),
            AggregateId = dto.ActivityId.ToString(),
            Type = "activity.checkin",
            Data = dto
        };
        await _ingress.PublishDomainEventAsync(evt, ct);

        return dto;
    }
}
```

---

MassTransit req/resp (Prepare/Commit/Rollback) — Commit consumer örneği
- Pattern: Prepare -> Commit -> Rollback. Realtime bildirimleri yalnızca Commit aşamasında yapılmalı.

Concrete consumer (commit aşamasında Realtime):
```csharp
public class ActivityCreatedRealtimeConsumer : AizenBaseMessageConsumerWithResult<ActivityCreatedMessage, ActivityCreatedMessageResult>
{
    private readonly IRealtimeEventIngress _ingress;

    public ActivityCreatedRealtimeConsumer(IServiceProvider sp) : base(sp)
    {
        _ingress = sp.GetRequiredService<IRealtimeEventIngress>();
    }

    public override Task<bool> ExecutePrepareMessage(ActivityCreatedMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);

    public override async Task<ActivityCreatedMessageResult> ExecuteCommitMessage(ActivityCreatedMessage message, CancellationToken cancellationToken)
    {
        var evt = new EventDto
        {
            Id = message.CorrelationId ?? Guid.NewGuid().ToString(),
            AggregateId = message.ActivityId.ToString(),
            Type = "activity.created",
            Data = message.Payload
        };

        await _ingress.PublishDomainEventAsync(evt, cancellationToken);

        return new ActivityCreatedMessageResult { Id = evt.Id, IsSuccess = true };
    }

    public override Task ExecuteRollbackMessage(ActivityCreatedMessage message, AizenMessageError ex, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

MassTransit registration (AddAizenMessagebus içinde discovery veya explicit AddConsumer):
```csharp
x.AddConsumer<ActivityCreatedRealtimeConsumer>();
```

---

IEventSocketMapper (module-level) örneği
- Mapper: EventDto veya domain event → RealtimeMessage dönüşümü ve hedeflerin belirlenmesi.

```csharp
public class ActivityEventSocketMapper : IEventSocketMapper
{
    public RealtimeMessage? Map(object domainEvent)
    {
        if (domainEvent is EventDto dto && dto.Type.StartsWith("activity."))
        {
            return new RealtimeMessage
            {
                Type = dto.Type,
                Stream = $"activity:{dto.AggregateId}",
                AggregateId = dto.AggregateId,
                Payload = dto.Data,
                CorrelationId = dto.Id
            };
        }
        return null;
    }

    public (IEnumerable<string> UserIds, IEnumerable<string> GroupNames) GetTargets(object domainEvent)
    {
        if (domainEvent is EventDto dto)
        {
            // Örnek: tüm activity katılımcıları / tüm bağlananlar için activity group
            return (Array.Empty<string>(), new[] { $"activity:{dto.AggregateId}" });
        }
        return (Array.Empty<string>(), Array.Empty<string>());
    }
}
```

---

RealtimeIngressService (işleyiş)
- RealtimeIngressService.PublishDomainEventAsync(EventDto):
  1. Mapper ile RealtimeMessage oluşturur.
  2. Mapper.GetTargets ile hedefleri alır (userIds, groupNames).
  3. IRealtimePublisher aracılığıyla PublishToGroupAsync / PublishToUserAsync / PublishToChannelAsync çağrılır.

---

Client (JS) örneği
- SignalR client (browser) — JWT token kullanan örnek:

```javascript
import * as signalR from "@microsoft/signalr";

const token = "<JWT_TOKEN>";
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/activity", { accessTokenFactory: () => token })
  .withAutomaticReconnect()
  .build();

connection.on("ReceiveEvent", (payload) => {
  console.log("Realtime event", payload);
});

connection.on("ReceiveChatMessage", (msg) => {
  console.log("Chat", msg);
});

await connection.start();
await connection.invoke("JoinActivityRoom", 123); // join activity:123
```

---

Akış: nerede/ ne zaman tetiklenir
- Command işlenip DB commit edildikten sonra:
  - Eğer direct ingress pattern: handler PublishDomainEventAsync çağırır -> anında client'lara gönderim.
  - Eğer MessageBus pattern: handler domain event'i messagebus'a publish eder -> Prepare/Commit flow yürür -> Consumer commit aşamasında Realtime'a bildirir.
- Client join işlemi Hub metodunu çağırdığında (JoinActivityRoom) hub içindeki authorization servisi kontrolü yapar -> Groups.AddToGroupAsync ile grup üyesi olur.
- HubFilter: tüm hub invokasyonlarında çalışır (rate-limit, message length).

---

Test & QA (kısa)
- Unit:
  - Mapper test: EventDto -> RealtimeMessage doğruluk
  - Publisher test: Mock ISocketManager ile Publish* metodlarının doğru çağrıldığını doğrulayın
- Integration:
  - local RabbitMQ + MassTransit TestHarness + SignalR server: prepare/commit/commitConsumer -> client ReceiveEvent doğrulama
- Load:
  - Simüle çok sayıda connection + mesaj: rate-limit davranışı, backplane darboğazları test edin

---

Nerede hangi dosya/klasör
- Core/Realtime/src/Aizen.Core.Realtime.Abstraction/... (models/interfaces)
- Core/Realtime/src/Aizen.Core.Realtime/... (services, hubs, filters, extensions)
- Core/Realtime/docs/README-realtime.md (bu dosya)
- Modules/Activity/... (ModuleActivityHub, mapper, authorizer, registrar, Program.cs)

---

Sonuç ve öneriler
- Komut tabanlı (Command handler) veya CQRS/eventbus tabanlı (MassTransit commit consumer) yaklaşımdan size en uygun olanı seçin:
  - Basit / hızlı: handler içinde direct PublishDomainEventAsync
  - Güvenilir / transactional: outbox + MassTransit commit consumer -> Realtime
- Prod için:
  - Redis backplane veya Azure SignalR Service
  - Redis-based rate-limiter ve presence store
  - Telemetry / metrics (AppInsights / Prometheus)

Eğer isterseniz:
- Bu README'yi repo'ya `.md` olarak ekleyecek şekilde bir PR açabilirim,
- Ya da örnek consumer/handler/test projelerini module path'lerine göre doldurup zip hazırlayabilirim.
