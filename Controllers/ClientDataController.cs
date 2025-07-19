
using AutoMapper;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientDataController(IUnitOfWork unitOfWork, IMapper mapper) : BaseAPIController
    {
        [HttpGet]
        public async Task<List<WorkoutDataDto>> GetAllDailyClientSessions(string date)
        {
            var clientSessions = await unitOfWork.ClientDataRepository.GetClientRecordsByDateAsync(DateOnly.Parse(date));
            var clientMappedSessions = new List<WorkoutDataDto>();

            if (clientSessions == null) throw new Exception($"No client sessions found on specificed date: {date}");

            foreach (var clientSession in clientSessions)
            {
                var clientDataDto = mapper.Map<WorkoutDataDto>(clientSession);
                clientMappedSessions.Add(clientDataDto);

            }
            return clientMappedSessions;

        }

        [HttpGet("{clientName}/current")]
        public async Task<int> GetCurrentClientBlockSession(string clientName)
        {
            var clientSession = await unitOfWork.ClientDataRepository.GetClientsLastSessionAsync(clientName);

            if (clientSession == null) throw new Exception($"{clientName} was not found");

            return clientSession.CurrentBlockSession;
        }

        [HttpGet("{clientName}/lastDate")]
        public async Task<DateOnly> GetLatestClientSessionDate(string clientName)
        {
            var clientSession = await unitOfWork.ClientDataRepository.GetClientsLastSessionAsync(clientName);

            if (clientSession == null) throw new Exception($"{clientName} was not found");
            return clientSession.SessionDate;


        }

        [HttpGet("onLastLast")]
        public async Task<List<string>> GetClientsOnLastBlockSession()
        {
            var clientSessions = await unitOfWork.ClientDataRepository.GetClientsOnLastSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their last block session");
            return clientSessions;
        }

        [HttpGet("onFirstSession")]
        public async Task<List<string>> GetClientsOnFirstBlockSession()
        {
            var clientSessions = await unitOfWork.ClientDataRepository.GetClientsOnFirstSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their first block session");
            return clientSessions;
        }

    }
}
