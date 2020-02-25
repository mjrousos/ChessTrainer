using System;
using Microsoft.Azure.Cosmos.Table;

namespace IngestionFunctions.Models
{
    public class IngestionRecord : TableEntity
    {
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
