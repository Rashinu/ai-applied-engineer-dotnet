using LlmFinOpsCopilot.Infrastructure;
using Microsoft.EntityFrameworkCore;
using LlmFinOpsCopilotDomain;
using LlmFinOpsCopilot.Forecasting;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;


double totalPercentError = 0;

var optionsBuilder = new DbContextOptionsBuilder<LlmDbContext>()
  .UseNpgsql("Host=localhost;Port=5433;Database=llmfinopscopilotdb;Username=postgres;Password=postgres")
  .Options;

using var db = new LlmDbContext(optionsBuilder);

var hourly = (await db.LlmCallLogs
        .GroupBy(log => new { log.Timestamp.Year, log.Timestamp.Month, log.Timestamp.Day, log.Timestamp.Hour })
        .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, Cost = g.Sum(log => log.Cost) })
        .ToListAsync())
    .Select(x => new HourlyCostPoint(new DateTime(x.Year, x.Month, x.Day, x.Hour, 0, 0), x.Cost))
    .OrderBy(point => point.Timestamp)
    .ToList();
var actuals = hourly.TakeLast(24).ToList(); // Last 24 hours for comparison
foreach (var point in hourly.Take(10))
{
    Console.WriteLine($"Timestamp: {point.Timestamp}, Cost: {point.Cost}");
}

var mlContext = new MLContext();

var inputs = hourly.TakeLast(168).Select(point => new CostInput { Cost = (float)point.Cost }).ToList();
var trainCount = inputs.Count - 24; // Use all but the last 24 hours for training
var trainData = mlContext.Data.LoadFromEnumerable(inputs.Take(trainCount));

var pipeline = mlContext.Forecasting.ForecastBySsa(
    outputColumnName: nameof(CostForecast.Forecast),
    inputColumnName: nameof(CostInput.Cost),
    windowSize: 24,
    seriesLength: trainCount,
    trainSize: trainCount,
    horizon: 24);


var model = pipeline.Fit(trainData);

var forecastEngine = model.CreateTimeSeriesEngine<CostInput, CostForecast>(mlContext);
var forecast = forecastEngine.Predict();

for (int i = 0; i < forecast.Forecast.Length; i++)
{
    totalPercentError += Math.Abs((forecast.Forecast[i] - (double)actuals[i].Cost) / (double)actuals[i].Cost) * 100;
    Console.WriteLine($"Forecasted Cost for {actuals[i].Timestamp}: {forecast.Forecast[i]}, Actual Cost: {actuals[i].Cost}");
}
Console.WriteLine($"MAPE: {totalPercentError / actuals.Count:F2}%");

public class CostInput
{
    public float Cost { get; set; }
}

public class CostForecast
{
    public float[] Forecast { get; set; } = [];
}