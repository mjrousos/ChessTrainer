using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MjrChess.Trainer.Data.Models
{
    public class IEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTimeOffset? CreatedDate { get; set; }

        public DateTimeOffset? LastModifiedDate { get; set; }
    }
}
