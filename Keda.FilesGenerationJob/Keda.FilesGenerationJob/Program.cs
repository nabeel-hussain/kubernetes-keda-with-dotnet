using CsvHelper;
using StackExchange.Redis;
using System.Globalization;
using System.Threading;class CsvGeneratorJob
{
    static void Main()
    {
        Console.WriteLine("Starting CSV file generation...");

        // Load environment variables
        string? redisHost = Environment.GetEnvironmentVariable("REDIS_HOST");
        string? redisList = Environment.GetEnvironmentVariable("REDIS_LIST");

        if (string.IsNullOrEmpty(redisHost) || string.IsNullOrEmpty(redisList))
        {
            Console.WriteLine($"ERROR: Missing REDIS_HOST or REDIS_LIST. REDIS_HOST: {redisHost}, REDIS_LIST: {redisList}");
            return;
        }
        Console.WriteLine($"REDIS_LIST. REDIS_HOST: {redisHost}, REDIS_LIST: {redisList}");

        // Initialize Redis connection
        ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(redisHost);
        IDatabase db = redis.GetDatabase();

        // Set directory for CSV files
        string directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "data", "raw");
        if (!Directory.Exists(directory))
        {
            Console.WriteLine("Creating the directory, it does not exist");
            Directory.CreateDirectory(directory);
        }

        // Define start and end dates
        DateTime startDate = new DateTime(2020, 1, 1);
        DateTime endDate = new DateTime(2020, 6, 1);

        // Iterate over each date
        DateTime currentDate = startDate;
        Random random = new Random();

        while (currentDate < endDate)
        {
            // Generate filename
            string filename = currentDate.ToString("yyyyMMdd") + ".csv";
            string filepath = Path.Combine(directory, filename);
            Console.WriteLine("Creating file");
            Thread.Sleep(2000);
            // Generate CSV data
            List<SalesRecord> records = new List<SalesRecord>();
            for (int i = 0; i < 200; i++)
            {
                records.Add(new SalesRecord
                {
                    Timestamp = currentDate.AddSeconds(random.Next(0, 86400)),
                    ItemId = random.Next(1, 6),
                    QuantitySold = random.Next(1, 11)
                });
            }

            // Write data to CSV
            using (var writer = new StreamWriter(filepath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(records);
            }

            // Push filename to Redis list
            db.ListLeftPush(redisList, filename);

            // Move to the next day
            currentDate = currentDate.AddDays(1);
        }

        Console.WriteLine("Finished CSV file generation.");
    }

    // Model for CSV records
    public class SalesRecord
    {
        public DateTime Timestamp { get; set; }
        public int ItemId { get; set; }
        public int QuantitySold { get; set; }
    }
}
