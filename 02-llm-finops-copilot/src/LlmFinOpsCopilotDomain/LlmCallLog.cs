namespace LlmFinOpsCopilotDomain
{
    public class LlmCallLog
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Prompt { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public string Provider { get; set; } =  string.Empty;

        public string Model { get; set; } = string.Empty;

        public int PromptTokenCount { get; set; }

        public int CompletionTokenCount { get; set; }

        public double LatencyMs { get; set; }

        public bool CacheHit { get; set; }

        public string Team  { get; set; } = string.Empty;

        public decimal Cost { get; set; }

    }
}