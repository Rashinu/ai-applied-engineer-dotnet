using System.ComponentModel;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using ReleaseRiskAnalyst.Domain;
using ReleaseRiskAnalyst.Infrastructure;

[McpServerToolType]
public static class ReleaseRiskTools
{
    [McpServerTool]
    [Description("Belirli bir servisin release sinyallerini döner.")]
    public static async Task<List<ReleaseSignal>> GetReleaseSignals(
        ReleaseRiskDbContext db,
        string serviceName)
    {
        serviceName = serviceName.Trim().ToLowerInvariant();
        return await db.ReleaseSignals.Where(rs => rs.ServiceName == serviceName).ToListAsync();
    }
    [McpServerTool]
    [Description("Belirli bir servisin incident geçmişini döner.")]
    
    public static async Task<List<ServiceIncidentHistory>> GetServiceIncidentHistories(
        ReleaseRiskDbContext db,
        string serviceName)
    {
        serviceName = serviceName.Trim().ToLowerInvariant();
        return await db.ServiceIncidentHistories.Where(si => si.ServiceName == serviceName).ToListAsync();
    }
}
