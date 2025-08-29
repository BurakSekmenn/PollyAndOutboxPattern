using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Graph;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Invoice.Winforms
{
    public partial class MainForm : Form
    {

        private IHost? _webHost;
        public MainForm()
        {
            InitializeComponent();
            comboMode.Items.AddRange(new[] { "Normal", "Always500", "Always429", "Slow" });
            comboMode.SelectedIndex = 0;
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (_webHost != null) return;

            var mode = comboMode.SelectedItem?.ToString() ?? "Normal";

            _webHost = CreateWebHost(mode);
            await _webHost.StartAsync();
            lblStatus.Text = "Running at http://localhost:5057";
        }
        private async void btnStop_Click(object sender, EventArgs e)
        {
            if (_webHost == null) return;

            await _webHost.StopAsync();
            _webHost.Dispose();
            _webHost = null;
            lblStatus.Text = "Stopped";
        }

        private IHost CreateWebHost(string mode)
        {
            return Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Portu burada sabitle
                    webBuilder.ConfigureKestrel(o => o.ListenLocalhost(5057));

                    // Endpoint routing için gerekli
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                    });

                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();

                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapPost("/api/invoices", async ctx =>
                            {
                                switch (mode)
                                {
                                    case "Always500":
                                        ctx.Response.StatusCode = 500;
                                        await ctx.Response.WriteAsync("Simulated 500");
                                        return;
                                    case "Always429":
                                        ctx.Response.StatusCode = 429;
                                        await ctx.Response.WriteAsync("Simulated 429");
                                        return;
                                    case "Slow":
                                        await Task.Delay(5000); // timeout testi
                                        break;
                                    default:
                                        await Task.Delay(Random.Shared.Next(100, 400));
                                        break;
                                }

                                using var sr = new StreamReader(ctx.Request.Body);
                                var text = await sr.ReadToEndAsync();

                                // istersen payload'ı parse edebilirsin:
                                JsonElement? payload = null;
                                if (!string.IsNullOrWhiteSpace(text))
                                    payload = JsonDocument.Parse(text).RootElement;

                                var invoiceNo = $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

                                await ctx.Response.WriteAsJsonAsync(new
                                {
                                    ok = true,
                                    invoiceNo,
                                    received = payload
                                });
                            });
                        });
                    });
                })
                .Build();
        }


    }
}
