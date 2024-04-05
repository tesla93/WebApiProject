using Core.Crud.Interfaces;
using Project.Data.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Services.Interfaces
{
    public interface IOrderService: IPagedCrudService<OrderDTO>
    {
    }
}
