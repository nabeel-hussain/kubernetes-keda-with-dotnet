using CsvHelper;
using StackExchange.Redis;
using System.Globalization;

class CsvAggregatorJob
{
    static void Main()
    {
        Console.WriteLine("Starting CSV aggregation...");

        // Load Redis environment variables
        string? redisHost = Environment.GetEnvironmentVariable("REDIS_HOST");
        string? redisList = Environment.GetEnvironmentVariable("REDIS_LIST");

        if (string.IsNullOrEmpty(redisHost) || string.IsNullOrEmpty(redisList))
        {
            Console.WriteLine($"ERROR: Missing REDIS_HOST or REDIS_LIST. REDIS_HOST: {redisHost}, REDIS_LIST: {redisList}");
            return;
        }

        Console.WriteLine($"REDIS_HOST: {redisHost}, REDIS_LIST: {redisList}");
        // Initialize Redis connection
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisHost);
        IDatabase db = redis.GetDatabase();

        // Read filename from Redis
        RedisValue lastCsvName = db.ListRightPop(redisList);
        if (lastCsvName.IsNullOrEmpty)
        {
            Console.WriteLine("No CSV files to process in the Redis queue. Exiting...");
            return;
        }

        string filenameRaw = lastCsvName.ToString();
        string inputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "data", "raw", filenameRaw);
        string processedDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "data", "processed");

        // Ensure processed directory exists
        if (!Directory.Exists(processedDirectory))
        {
            Console.WriteLine("Creating the directory, it does not exist");
            Directory.CreateDirectory(processedDirectory);
        }

        string outputFilename = Path.Combine(processedDirectory, $"{Path.GetFileNameWithoutExtension(filenameRaw)}_aggregated_sales.csv");

        // Read, aggregate, and write CSV
        AggregateCsv(inputPath, outputFilename);

        Console.WriteLine($"Aggregated sales data written to '{outputFilename}'.");
    }

    static void AggregateCsv(string inputFile, string outputFile)
    {
        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"ERROR: File {inputFile} not found.");
            return;
        }

        using (var reader = new StreamReader(inputFile))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<SalesRecord>().ToList();

            var aggregatedData = records
                .GroupBy(r => r.ItemId)
                .Select(g => new AggregatedSales { ItemId = g.Key, QuantitySold = g.Sum(r => r.QuantitySold) })
                .ToList();

            using (var writer = new StreamWriter(outputFile))
            using (var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csvWriter.WriteRecords(aggregatedData);
            }
        }
    }

    public class SalesRecord
    {
        public DateTime Timestamp { get; set; }
        public int ItemId { get; set; }
        public int QuantitySold { get; set; }
    }

    public class AggregatedSales
    {
        public int ItemId { get; set; }
        public int QuantitySold { get; set; }
    }
}
