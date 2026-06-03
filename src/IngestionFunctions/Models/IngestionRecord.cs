using System;
using Azure;
using Azure.Data.Tables;

namespace IngestionFunctions.Models
{
    public class IngestionRecord : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;

        public string RowKey { get; set; } = string.Empty;

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string ChessSite
        {
            get => PartitionKey;
            set => PartitionKey = value;
        }

        public string Player
        {
            get => RowKey;
            set => RowKey = value;
        }

        public DateTimeOffset MostRecentGame { get; set; }
    }
}
