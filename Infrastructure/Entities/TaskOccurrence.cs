using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Entities
{
    public class TaskOccurrence : BaseEntity<int>
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TaskTemplateId { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? CommittedDate { get; set; }

        [StringLength(1000)]
        public string Description { get; set; } //modified description for recurring

        [DataType(DataType.Date)]
        public DateTime? CompletionDate { get; set; }

        public TaskTemplate TaskTemplate { get; set; }
    }
}
