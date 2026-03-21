namespace MyFeatures.DTO
{
    public class TaskOccurrenceDto
    {
        public int Id { get; set; }
        public int TaskTemplateId { get; set; }
        public DateTime? CommittedDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Description { get; set; }
        public DateTime? CompletionDate { get; set; }
        public TaskTemplateDto TaskTemplate { get; set; }
    }

}
