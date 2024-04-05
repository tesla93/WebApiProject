using Core.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Data.Models
{
    public class Order: IEntity
    {
        public int Id { get; set; }
        public string PickupLocation { get; set; }
    }
}
