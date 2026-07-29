# Notification Modülü — V1.0.1 Roadmap
> Fiyat/abonelik, periyodik bakım ve dispute/chargeback yaşam döngüsü bildirimleri.
> Kanonik: `../COMMISSION_PACKAGE_PRICING.md`. Mevcut Notification altyapısı (list, header dropdown, Web Push/VAPID,
> deep-link, cache) **genişletilir** — yeni bildirim **tipleri** + tetikleri. Her faz → `BE_N<n>_*.md`.

## Backend fazları
| Faz | Kapsam | Kanonik § | Bağımlılık |
|---|---|---|---|
| **N1** | **Renewal fiyat değişikliği bildirimi** — abonelik renewal'ından **≥14 gün önce** provider'a (launch→list geçişi). | §13.2 | Payment P4 |
| **N2** | **Periyodik bakım hatırlatması** — `NextDueAt − ReminderLeadDays` tetikli (zehirli boya vb.). CargoDry renewal'dan ayrı. | §20.14 | ServiceRequest S12 |
| **N3** | **Dispute yaşam döngüsü bildirimleri** — tamamlama→müşteri; **oto-onay yaklaşıyor**→müşteri; **itiraz açıldı**→admin+provider; **çözüldü**→ikisi; **chargeback**→admin+provider. | §21.9 | ServiceRequest S13, Payment P10 |
| **N4** | **Entitlement / benefit event bildirimleri** (opsiyonel) — boost Active/Revoked, benefit budget uyarıları. | §9, §19.7 | Payment P6, P11 |

## FE
- **Provider portalı:** mevcut bildirim merkezi (list + push — **hazır**) yeni tipleri gösterecek + deep-link'ler
  (dispute case, abonelik, teklif). Ayrı büyük FE işi yok.
- **Admin panel:** dispute/chargeback **uyarı kuyruğu** bildirim tetikleriyle beslenir (Payment/SR admin ekranlarıyla).

## Açık kararlar (Notification)
- Kanal önceliği (in-app / push / e-posta / SMS) kalem bazında · oto-onay hatırlatma zamanlaması (3./6. gün öneri) ·
  chargeback bildiriminin müşteriye gidip gitmeyeceği.
