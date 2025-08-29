using ECommerce.Api.Context;
using ECommerce.Api.Entitiy;
using ECommerce.Api.Services;
using Microsoft.EntityFrameworkCore;
using Polly;                                  
using Polly.CircuitBreaker;
using Polly.Retry;                            
using Scalar.AspNetCore;                   
using System.Net;
using System.Text;
using System.Text.Json;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddCors();
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection")!,
        new MySqlServerVersion(new Version(8, 0, 36)),
        my => my.EnableRetryOnFailure());
});

builder.Services.AddHttpClient("InvoiceClient", c =>
{
    c.BaseAddress = new Uri("http://localhost:5057/"); // Invoice.Winforms uygulaması
})
.AddStandardResilienceHandler(o => // Polly ayarları
{
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5); // Tüm denemeler dahil
    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3); // Her deneme için
    o.Retry.MaxRetryAttempts = 3; // 3 yeniden deneme
    o.CircuitBreaker.MinimumThroughput = 6; // En az 6 istek
    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20); // 20 saniyelik pencere
    o.CircuitBreaker.FailureRatio = 0.5; // %50 hata
}); // 3. denemeden sonra devre kesici açılır ve 1 dakika boyunca istek atılmaz

// Outbox Worker
builder.Services.AddHostedService<OutboxWorker>();

var app = builder.Build();

app.UseCors(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()); 
app.MapOpenApi();
app.MapScalarApiReference();


app.MapPost("/orders/{id:guid}/invoice", async (Guid id, IHttpClientFactory f, AppDbContext db) =>
{
    var client = f.CreateClient("InvoiceClient");
    using var req = new HttpRequestMessage(HttpMethod.Post, "api/invoices")
    {
        Content = new StringContent(JsonSerializer.Serialize(new { orderId = id, amount = 149.90m }),
                                    Encoding.UTF8, "application/json")
    };
    req.Headers.TryAddWithoutValidation("Idempotency-Key", id.ToString("N"));

    try
    {
        var resp = await client.SendAsync(req);
        if (resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            return Results.Ok(new { status = (int)resp.StatusCode, body, mode = "direct" });
        }
    }
    catch { /* ağ hatası */ }

    // Outbox’a idempotent yaz (varsa güncelleme)
    var payload = JsonSerializer.Serialize(new { orderId = id, amount = 149.90m });
    var existing = await db.OutboxInvoices.FindAsync(id);
    if (existing is null)
    {
        db.OutboxInvoices.Add(new OutboxInvoice
        {
            OrderId = id,
            PayloadJson = payload,
            Attempt = 0,
            NextDueUtc = DateTime.UtcNow.AddMinutes(1),
            Status = OutboxStatus.Pending,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        });
    }
    else if (existing.Status is OutboxStatus.Pending or OutboxStatus.Processing)
    {
        // zaten kuyrukta; dokunma (idempotent)
    }
    else
    {
        existing.PayloadJson = payload;
        existing.Attempt = 0;
        existing.NextDueUtc = DateTime.UtcNow.AddMinutes(1);
        existing.Status = OutboxStatus.Pending;
        existing.UpdatedUtc = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();
    return Results.Accepted($"/outbox/{id}");
});

// Durum bak
app.MapGet("/outbox/{id:guid}", async (Guid id, AppDbContext db) =>
{
    var x = await db.OutboxInvoices.FindAsync(id);
    return x is null ? Results.NotFound() : Results.Ok(x);
});
app.MapGet("/outbox/failed", async (AppDbContext db) =>
{
    var items = await db.OutboxInvoices
        .Where(x => x.Status == OutboxStatus.Failed)
        .OrderByDescending(x => x.UpdatedUtc)
        .Take(100)
        .ToListAsync();
    return Results.Ok(items);
});

// Retry (FAILED → Pending) tek bir iş için
app.MapPost("/outbox/{id:guid}/retry", async (Guid id, AppDbContext db) =>
{
    var x = await db.OutboxInvoices.FindAsync(id);
    if (x is null) return Results.NotFound();
    x.Status = OutboxStatus.Pending;
    x.Attempt = 0;
    x.NextDueUtc = DateTime.UtcNow.AddMinutes(1);
    x.LastError = null;
    x.UpdatedUtc = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "queued-again" });
});

app.Run();
