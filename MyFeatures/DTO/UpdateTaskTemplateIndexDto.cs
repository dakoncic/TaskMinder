namespace MyFeatures.DTO
{
    public class UpdateTaskTemplateIndexDto
    {
        public int TaskTemplateId { get; set; }

        public int NewIndex { get; set; }

        public bool Recurring { get; set; }
    }
}
