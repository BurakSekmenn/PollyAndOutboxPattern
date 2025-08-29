# PollyAndOutboxPattern

Bu repo, **.NET 9**, **Polly** ve **Outbox Pattern** kullanarak dayanıklı bir fatura sistemi demo’su sunar.  
WinForms tabanlı sahte bir servis (“Invoice Service”) ve MySQL Outbox ile, servisin düşmesi durumunda faturanın kaybolmamasını sağlıyoruz.

---

## 📂 İçerik Yapısı

- **Invoice.Winforms** – Aç/kapa yapabileceğin sahte invoice servisi.  
- **ECommerce.Api** – Polly destekli HttpClient ve MySQL Outbox ile asenkron fatura işleme API’si.

---

## 🔄 Çalışma Akışı

1. Kullanıcı `POST /orders/{id}/invoice` ile siparişe fatura isteği gönderir.  
2. API, Polly ile birlikte dış Invoice servisine (WinForms) çağrı yapar:
   - Başarırsa: doğrudan fatura oluşturulur.  
   - Başarısız olursa (timeout, hata, circuit open vs.): istek MySQL Outbox'a yazılır.  
3. Background Worker, Outbox'taki kayıtları periyodik olarak kontrol eder:
   - Başarılıysa `Status = Succeeded`.  
   - Max deneme sayısını aşarsa `Status = Failed`.  
   - Aksi takdirde tekrar denenir (exponential backoff).

---

## 📊 Test Senaryoları & DB Sonuçları

| Senaryo                   | Outbox Durumu     | Açıklama                                 |
|---------------------------|-------------------|-------------------------------------------|
| Normal                    | Succeeded veya yok| Polly retry gerekmedi, fatura direkt kesildi |
| Sunucu kapalı (Stop)      | Pending           | Outbox'a yazıldı, worker deneyecek         |
| Retry → Servis açıldı     | Succeeded         | Başarılı deneme sonrası fatura kesildi     |
| TimeoutException          | LastError=Timeout | Deneme süresi doldu, Outbox tekrar deneyecek |
| Max deneme sonrası hata   | Failed            | `max-attempts (TimeoutRejectedException)` olarak kayıtlandı |
| Circuit açıkken çağrı     | Pending           | Polly BrokenCircuit, Outbox bekletiyor      |

---

## 📝 Kod Örneği – Polly Resilience Handler

```csharp
builder.Services.AddHttpClient("InvoiceClient")
  .AddStandardResilienceHandler(o =>
  {
      o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
      o.AttemptTimeout.Timeout      = TimeSpan.FromSeconds(3);
      o.Retry.MaxRetryAttempts      = 3;
      o.CircuitBreaker.MinimumThroughput = 6;
      o.CircuitBreaker.FailureRatio      = 0.5;
  });
```

---

## 🚀 Başlangıç

1. `appsettings.json` içindeki MySQL bağlantısını (`ConnectionStrings:Default`) düzenle.  
2. API projesinde migration’ları çalıştır:
   ```bash
   dotnet ef migrations add Initial
   dotnet ef database update
   ```
3. WinForms projesini çalıştır ve "Start" ile servisi ayağa al.  
4. API’yi çalıştır: 
   ```bash
   dotnet run --project ECommerce.Api
   ```
5. Postman/Swagger ile test et:
   - Başarılı/NORMAL: fatura doğrudan kesilsin.  
   - SERVICE KAPALI ile `Pending`, sonra servis açıldığında `Succeeded`.  
   - ALWAYS500 ile max deneme sonrası `Failed`.  
   - `POST /outbox/{id}/retry` ile manuel retry yap.

---

## 🛠 Teknolojiler

- .NET 9 (.NET 9.0)  
- Polly (Retry / CircuitBreaker / Timeout)  
- Microsoft.Extensions.Http.Resilience  
- EF Core + Pomelo MySQL  
- WinForms (fake invoice service)

---
