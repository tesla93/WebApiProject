using Core.Crud;
using Core.Membership;
using Core.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Project.Data.DTO;
using Project.Services.Interfaces;

namespace BBWT.Server.Api
{
    [Produces("application/json")]
    [Route("api/order")]
    [ReadWriteAuthorize(ReadRoles = Roles.SystemAdminRole + "," + Roles.SuperAdminRole, WriteRoles = Roles.SystemAdminRole)]
    public class OrderController : PagedCrudControllerBase<OrderDTO>
    {
        private readonly IOrderService _orderService;


        public OrderController(
            IOrderService orderService,
            ILogger<OrderController> logger) : base(orderService, logger)
        {
            this._orderService = orderService;
        }


    }
}