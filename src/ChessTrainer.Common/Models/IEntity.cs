using System;

namespace MjrChess.Trainer.Models
{
    public class IEntity
    {
        public int Id { get; set; }

        public DateTimeOffset? CreatedDate { get; set; }

        public DateTimeOffset? LastModifiedDate { get; set; }
    }
}
