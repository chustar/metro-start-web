using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;

namespace MetroStart
{
   public class  ThemeEntity : TableEntity
    {
        public ThemeEntity(string author, string title, bool online, Dictionary<string, string> themeContent)
        {
            PartitionKey = author;
            RowKey = title;
            Online = online;
            ThemeContent = themeContent;
        }

        public ThemeEntity()
        {
        }

        public string Author => PartitionKey;
        public string Title => RowKey;
        public bool Online { get; set; }
        public string ThemeContentJson { get; set; }
        public Dictionary<string, string> ThemeContent
        {
            get => JsonConvert.DeserializeObject<Dictionary<string, string>>(ThemeContentJson);
            set => ThemeContentJson = JsonConvert.SerializeObject(value);
        }

        public override string ToString() => $"(Author: {Author}, Title: {Title})";

        public static async Task<CloudTable> GetCloudTable(ILogger log)
        {
            if (System.Environment.GetEnvironmentVariable("METROSTART_TABLE_CONNECTION_STRING", EnvironmentVariableTarget.Process) is var connectionString)
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("themes");

                // Create the table if it doesn't exist.
                await table.CreateIfNotExistsAsync();

                return table;
            }

            throw new ApplicationException("Could not find connectionString.");
        }

        public static ThemeEntity CreateThemeEntity(IDictionary<string, string> flatTheme)
        {
            string author = null;
            string title = null;
            bool online = true;
            Dictionary<string, string> themeContent = new Dictionary<string, string>();
            foreach (var (Key, Value) in flatTheme)
            {
                if (Key.Equals("author", StringComparison.InvariantCultureIgnoreCase))
                {
                    author = Value;
                }
                else if (Key.Equals("title", StringComparison.InvariantCultureIgnoreCase))
                {
                    title = Value;
                }
                else if (Key.Equals("online", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!bool.TryParse(Value, out online))
                    {
                        online = true;
                    }
                }
                else
                {
                    themeContent.Add(Key, Value);
                }
            }

            _ = author.Nullable() ?? throw new ArgumentNullException(nameof(author));
            _ = title.Nullable() ?? throw new ArgumentNullException(nameof(title));

            return new ThemeEntity(author, title, online, themeContent);
        }

        public static async Task<ThemeEntity> InsertTheme(ThemeEntity themeEntity, CloudTable table, ILogger log)
        {
            TableOperation insertOperation = TableOperation.Insert(themeEntity);

            // Execute the insert operation.
            log.LogDebug($"Saving new theme {themeEntity}");
            return (await table.ExecuteAsync(insertOperation))?.Result as ThemeEntity ?? throw new InvalidDataException($"Theme {themeEntity} was not saved");
        }

        public static async Task<List<ThemeEntity>> GetAllThemes(ILogger log)
        {
            var table = await ThemeEntity.GetCloudTable(log);
            TableContinuationToken tableToken = null;

            var results = new List<ThemeEntity>();
            do
            {
                var segment = await table.ExecuteQuerySegmentedAsync(new TableQuery<ThemeEntity>(), tableToken) ?? throw new ApplicationException("No segment was returned.");
                results.AddRange(segment.Results);
                tableToken = segment.ContinuationToken;
            } while (tableToken != null);

            return results;
        }

        public static async Task<bool> ThemeExists(string title, ILogger log)
        {
            var table = await ThemeEntity.GetCloudTable(log);
            var results = await table.ExecuteQuerySegmentedAsync(
                new TableQuery<ThemeEntity>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, title)),
                null) ?? throw new ApplicationException("No segment was returned.");

            return results?.Results.Count > 0;
        }
    }
}