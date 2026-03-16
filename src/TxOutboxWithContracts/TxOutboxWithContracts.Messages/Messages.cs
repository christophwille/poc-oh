namespace TxOutboxWithContracts.Messages
{
    // Ideally those would sport idempotency key and deduplication id

    public class BlogCreated
    {
        public string Id { get; set; }
        public string Url { get; set; }
    }

    public class PostCreated
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
}
