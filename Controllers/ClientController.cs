using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientController(IUnitOfWork unitOfWork) : BaseAPIController
    {


        [HttpGet("{clientName}/currentSession")]
        public async Task<int> GetCurrentClientBlockSession(string clientName)
        {
            var clientSession = await unitOfWork.ClientDataRepository.GetClientByNameAsync(clientName);

            if (clientSession == null) throw new Exception($"{clientName} was not found");

            return clientSession.CurrentBlockSession;
        }

        [HttpGet("/onLastSession")]
        public async Task<List<string>> GetClientsOnLastBlockSession()
        {
            var clientSessions = await unitOfWork.ClientDataRepository.GetClientsOnLastSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their last block session");
            return clientSessions;
        }

        [HttpGet("/onFirstSession")]
        public async Task<List<string>> GetClientsOnFirstBlockSession()
        {
            var clientSessions = await unitOfWork.ClientDataRepository.GetClientsOnFirstSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their first block session");
            return clientSessions;
        }

    }
}
