using Core.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Data.DTO
{
    public class OrderDTO: IDTO
    {
        public int Id { get; set; }
        public string PickupLocation { get; set; }
    }
}
