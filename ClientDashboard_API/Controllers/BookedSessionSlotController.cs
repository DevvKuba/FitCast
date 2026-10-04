using AutoMapper;
using ClientDashboard_API.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace ClientDashboard_API.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class BookedSessionSlotController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
    }
}
