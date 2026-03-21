namespace MyFeatures.DTO
{
    public class TaskOccurrenceDto
    {
        public int Id { get; set; }
        public int TaskTemplateId { get; set; }
        public DateTime? CommittedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? CompletionDate { get; set; }
        public required TaskTemplateDto TaskTemplate { get; set; }
    }

}
