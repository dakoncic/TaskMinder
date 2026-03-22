using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class TaskOccurrence : BaseEntity
    {
        [Required]
        public int TaskTemplateId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CommittedDate { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? CompletionDate { get; set; }

        public TaskTemplate TaskTemplate { get; set; } = null!;
    }
}
