using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientController(IUnitOfWork unitOfWork) : BaseAPIController
    {

        /// <summary>
        /// Client method allowing for the retrieval of the clients current session,
        /// within their respective block
        /// </summary>
        [HttpGet("{clientName}/currentSession")]
        public async Task<int> GetCurrentClientBlockSession(string clientName)
        {
            var clientSession = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (clientSession == null) throw new Exception($"{clientName} was not found");

            return clientSession.CurrentBlockSession;
        }


        /// <summary>
        /// Client method allowing for the retrieval of all clients, on their last block session
        /// </summary>
        [HttpGet("/onLastSession")]
        public async Task<List<string>> GetClientsOnLastBlockSession()
        {
            var clientSessions = await unitOfWork.ClientRepository.GetClientsOnLastSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their last block session");
            return clientSessions;
        }

        /// <summary>
        /// Client method allowing for the retrieval of all clients 
        /// </summary>
        [HttpGet("/onFirstSession")]
        public async Task<List<string>> GetClientsOnFirstBlockSession()
        {
            var clientSessions = await unitOfWork.ClientRepository.GetClientsOnFirstSessionAsync();

            if (clientSessions == null) throw new Exception("No clients currently on their first block session");
            return clientSessions;
        }

        [HttpPost]
        public async Task<ActionResult> AddNewClient(string clientName)
        {
            await unitOfWork.ClientRepository.AddNewClientAsync(clientName);
            if (await unitOfWork.Complete()) return Ok($"Client: {clientName} added");

            return BadRequest($"Client {clientName} not added");
        }

        [HttpDelete]
        public async Task<ActionResult> RemoveClient(string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            unitOfWork.ClientRepository.RemoveClient(client);
            if (await unitOfWork.Complete()) return Ok($"Client: {clientName} removed");

            return BadRequest($"Client {clientName} not removed");
        }

    }
}
