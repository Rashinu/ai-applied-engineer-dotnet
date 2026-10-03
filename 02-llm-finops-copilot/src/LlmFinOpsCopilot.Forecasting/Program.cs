using LlmFinOpsCopilot.Infrastructure;
using Microsoft.EntityFrameworkCore;
using LlmFinOpsCopilotDomain;
using LlmFinOpsCopilot.Forecasting;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

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

var mlContext = new MLContext();

const int trainLength = 672; // Her pencerede eğitim uzunluğu (4 hafta)
const int horizon = 24;      // Her pencerede tahmin ufku
const int windowCount = 5;   // Kaç kesim noktası
const double spikeMultiplier = 3; // Sıçrama eşiği: medyanın kaç katı

var mapes = new List<double>();

for (int w = 0; w < windowCount; w++)
{
    // Kesim noktası: sondan başlayarak her turda 24 saat geriye
    int originIndex = hourly.Count - horizon - w * horizon;

    // Eğitim: kesim noktasından ÖNCEKİ trainLength saat (kesim noktası dahil değil)
    var trainSlice = hourly.Skip(Math.Max(0, originIndex - trainLength)).Take(trainLength).ToList();

    // Gerçek değerler: kesim noktasından itibaren horizon saat
    var actualSlice = hourly.Skip(originIndex).Take(horizon).ToList();

    // Eğitim verisindeki sıçramaları medyanla değiştir (eşik: medyanın 3 katı)
    var sortedCosts = trainSlice.Select(p => (double)p.Cost).OrderBy(c => c).ToList();
    double median = sortedCosts[sortedCosts.Count / 2];
    double spikeThreshold = spikeMultiplier * median;

    var trainInputs = trainSlice
        .Select(point => new CostInput
        {
            Cost = (float)((double)point.Cost > spikeThreshold ? median : (double)point.Cost)
        })
        .ToList();
    var trainData = mlContext.Data.LoadFromEnumerable(trainInputs);

    var pipeline = mlContext.Forecasting.ForecastBySsa(
        outputColumnName: nameof(CostForecast.Forecast),
        inputColumnName: nameof(CostInput.Cost),
        windowSize: 168,
        seriesLength: trainLength,
        trainSize: trainLength,
        horizon: horizon);

    var forecastEngine = pipeline.Fit(trainData).CreateTimeSeriesEngine<CostInput, CostForecast>(mlContext);
    var forecast = forecastEngine.Predict();

    // Bu pencerenin MAPE'si
    double totalError = 0;
    for (int i = 0; i < horizon; i++)
    {
        double actual = (double)actualSlice[i].Cost;
        totalError += Math.Abs((forecast.Forecast[i] - actual) / actual);
    }
    double mape = totalError / horizon * 100;
    mapes.Add(mape);

    for (int i = 0; i < horizon; i++)
    {
        Console.WriteLine($"    {actualSlice[i].Timestamp:dd.MM HH:mm}  tahmin: {forecast.Forecast[i],8:F2}  gerçek: {actualSlice[i].Cost,8:F2}");
    }
    Console.WriteLine($"  kesim: {hourly[originIndex].Timestamp}  eğitim max: {trainSlice.Max(p => p.Cost):F2}  test max: {actualSlice.Max(p => p.Cost):F2}");

    Console.WriteLine($"Pencere {w + 1}: MAPE = {mape:F2}%");
}

Console.WriteLine($"Ortalama MAPE: {mapes.Average():F2}%");
Console.WriteLine($"En iyi: {mapes.Min():F2}%  En kötü: {mapes.Max():F2}%");

public class CostInput
{
    public float Cost { get; set; }
}

public class CostForecast
{
    public float[] Forecast { get; set; } = [];
}
