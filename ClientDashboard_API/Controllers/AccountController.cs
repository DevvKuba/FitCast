using ClientDashboard_API.DTOs;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class AccountController(IUnitOfWork unitOfWork, ITrainerRegisterService trainerRegsiterService) : BaseAPIController
    {
        [HttpPost]
        public async Task RegisterTrainer(RegisterDto registerInfo)
        {
            await trainerRegsiterService.Handle(registerInfo);
        }
    }
}
