using Core.DTO;

namespace Project.Data.DTO
{
    public class DocumentDTO : IDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public int ProjectId { get; set; }

        public string Base64 { get; set; }
        public string Project { get; set; }
    }
}
