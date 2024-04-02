using Core.DTO;

namespace Messages.Templates
{
    public class EmailTemplateParameterDTO: IDTO
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Notes { get; set; }
    }
}
