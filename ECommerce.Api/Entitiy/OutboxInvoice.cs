namespace ECommerce.Api.Entitiy
{
    public sealed class OutboxInvoice
    {
        public Guid OrderId { get; set; }           // Idempotency anahtarı
        public string PayloadJson { get; set; } = default!;
        public int Attempt { get; set; }
        public DateTime NextDueUtc { get; set; }
        public OutboxStatus Status { get; set; }
        public string? LastError { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
    }
    public enum OutboxStatus { Pending = 0, Processing = 1, Succeeded = 2, Failed = 3 }

}
