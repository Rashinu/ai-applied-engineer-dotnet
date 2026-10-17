namespace LlmFinOpsCopilotDomain
{
    public class AnomalyWindow
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        
        public Guid Id  { get; set; } = Guid.NewGuid();
    }
}