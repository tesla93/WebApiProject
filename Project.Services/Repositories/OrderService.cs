using AutoMapper;
using Core.Crud;
using Core.Data;
using Project.Data.DTO;
using Project.Data.Models;
using Project.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project.Services.Repositories
{
    public class OrderServices : PagedCrudService<Order, OrderDTO>, IOrderService
    {
        private readonly IDbContext _context;
        private readonly IMapper _mapper;
        public OrderServices(IDbContext context, IMapper mapper, bool useMappingOnQueries = false) : base(context, mapper, useMappingOnQueries)
        {
            _context = context;
            _mapper = mapper;
        }
    }
}
