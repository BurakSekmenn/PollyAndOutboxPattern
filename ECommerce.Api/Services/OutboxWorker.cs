
using ECommerce.Api.Context;
using ECommerce.Api.Entitiy;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ECommerce.Api.Services
{
    public class OutboxWorker(IServiceProvider sp,
    IHttpClientFactory f,
    ILogger<OutboxWorker> log) : BackgroundService
    {
        private const int MaxAttempts = 3; // 3 denemeden sonra FAILED
        protected override async Task ExecuteAsync(CancellationToken token)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
            while (await timer.WaitForNextTickAsync(token))
            {
                try
                {
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var client = f.CreateClient("InvoiceClient");

                    // 1) Due batch çek (Pending && NextDueUtc <= now)
                    var now = DateTime.UtcNow;
                    var due = await db.OutboxInvoices
                        .Where(x => x.Status == OutboxStatus.Pending && x.NextDueUtc <= now)
                        .OrderBy(x => x.NextDueUtc)
                        .Take(50)
                        .ToListAsync(token);

                    if (due.Count == 0) continue;

                    // 2) Processing’e al (race azaltmak için hızlı update)
                    foreach (var i in due)
                    {
                        i.Status = OutboxStatus.Processing;
                        i.UpdatedUtc = DateTime.UtcNow;
                    }
                    await db.SaveChangesAsync(token);

                    // 3) Gönder
                    foreach (var item in due)
                    {
                        try
                        {
                            var req = new HttpRequestMessage(HttpMethod.Post, "api/invoices")
                            {
                                Content = new StringContent(item.PayloadJson, Encoding.UTF8, "application/json")
                            };
                            req.Headers.TryAddWithoutValidation("Idempotency-Key", item.OrderId.ToString("N"));

                            var resp = await client.SendAsync(req, token);
                            if (resp.IsSuccessStatusCode)
                            {
                                item.Status = OutboxStatus.Succeeded;
                                item.LastError = null;
                                item.UpdatedUtc = DateTime.UtcNow;
                                await db.SaveChangesAsync(token);
                                log.LogInformation("Invoice succeeded for {OrderId}", item.OrderId);
                            }
                            else
                            {
                                ScheduleRetry(item, $"HTTP {(int)resp.StatusCode}");
                                await db.SaveChangesAsync(token);
                                log.LogWarning("Retry scheduled for {OrderId}", item.OrderId);
                            }
                        }
                        catch (Exception ex)
                        {
                            ScheduleRetry(item, ex.GetType().Name);
                            await db.SaveChangesAsync(token);
                            log.LogWarning(ex, "Retry scheduled for {OrderId}", item.OrderId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Outbox worker tick failed");
                }
            }
        }
        static void ScheduleRetry(OutboxInvoice item, string error)
        {

            var nextAttempt = item.Attempt + 1;

            if (nextAttempt >= MaxAttempts)
            {
                item.Attempt = nextAttempt;
                item.Status = OutboxStatus.Failed;
                item.LastError = $"max-attempts ({error})";
             
                item.NextDueUtc = DateTime.UtcNow.AddYears(50);
                item.UpdatedUtc = DateTime.UtcNow;
                return;
            }

            item.Attempt = nextAttempt;
            item.Status = OutboxStatus.Pending;
            item.LastError = error;
            item.NextDueUtc = NextDue(item.Attempt); 
            item.UpdatedUtc = DateTime.UtcNow;
        }

        static DateTime NextDue(int attempt)
        {
        
            var minutes = Math.Min(15, Math.Pow(2, Math.Max(0, attempt - 1)));
            return DateTime.UtcNow.AddMinutes(minutes);
        }
    }
}
