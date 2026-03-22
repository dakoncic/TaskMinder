namespace MyFeatures.DTO
{
    public class CommitTaskOccurrenceDto
    {
        public int TaskOccurrenceId { get; set; }
        public DateTime? CommitDay { get; set; }
        public DateOnly LocalDate { get; set; }
    }

}
