using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientController(IUnitOfWork unitOfWork) : BaseAPIController
    {

        /// <summary>
        /// Client method allowing for the retrieval of the clients
        /// current session, within their respective block -
        /// endpoint: "/{clientName}/currentSession" from [ClientController]
        /// </summary>
        [HttpGet("{clientName}/currentSession")]
        public async Task<int> GetCurrentClientBlockSession(string clientName)
        {
            var clientSession = await unitOfWork.ClientDataRepository.GetClientByNameAsync(clientName);

            if (clientSession == null) throw new Exception($"{clientName} was not found");

            return clientSession.CurrentBlockSession;
        }


        /// <summary>
        /// Client method allowing for the retrieval of all clients,
        /// on their last block session -
        /// endpoint: "/onLastSession" from [ClientController]
        /// </summary>
        [HttpGet("/onLastSession")]
        public async Task<List<string>> GetClientsOnLastBlockSession()
        {
            var clientSessions = await unitOfWork.ClientDataRepository.GetClientsOnLastSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their last block session");
            return clientSessions;
        }

        /// <summary>
        /// Client method allowing for the retrieval of all clients,
        /// on their first block session -
        /// endpoint: "/onFirstSession" from [ClientController]
        /// </summary>
        [HttpGet("/onFirstSession")]
        public async Task<List<string>> GetClientsOnFirstBlockSession()
        {
            var clientSessions = await unitOfWork.ClientDataRepository.GetClientsOnFirstSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their first block session");
            return clientSessions;
        }

    }
}
