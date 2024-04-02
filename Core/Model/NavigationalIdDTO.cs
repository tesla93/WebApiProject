using Core.DTO;

namespace Core.Model
{

    public class NavigationalIdDTO<TEntityDTO, TKey>
    where TEntityDTO : class, IDTO<TKey>
    where TKey : IEquatable<TKey>
    {
        public TEntityDTO Previous { get; set; }
        public TEntityDTO Next { get; set; }
    }

}
