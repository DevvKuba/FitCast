
using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientDataController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
        [HttpGet]
        public async Task<List<WorkoutDataDto>> GetAllDailyClientSessions(string clientName)
        {
            //var clientSessions = unitOfWork.ClientDataRepository.Get
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<WorkoutDataDto> GetCurrentClientBlockSession(string clientName)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<WorkoutDataDto> GetLatestClientSessionDate(string clientName)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<List<string>> GetClientsOnLastSession()
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        public async Task<List<string>> GetClientsOnFirstSession()
        {
            throw new NotImplementedException();
        }

    }
}
