namespace TeamManagement.Application.DTOs.AI
{
    public class AiTaskUpdateResultDto
    {
        public Guid? GorevId { get; set; }
        public string GorevBasligi { get; set; }
        public string YeniDurum { get; set; }
        public string Not { get; set; }
    }
}
