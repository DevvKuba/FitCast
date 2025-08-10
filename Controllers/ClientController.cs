using ClientDashboard_API.Entities;
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
        public async Task<IActionResult> GetCurrentClientBlockSession(string clientName)
        {
            Client client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"{clientName} was not found");

            return Ok(client);
        }


        /// <summary>
        /// Client method allowing for the retrieval of all clients, on their last block session
        /// </summary>
        [HttpGet("/onLastSession")]
        public async Task<IActionResult> GetClientsOnLastBlockSession()
        {
            List<string> clientSessions = await unitOfWork.ClientRepository.GetClientsOnLastSessionAsync();

            if (clientSessions == null) return NotFound("No clients currently on their last block session");
            return Ok(clientSessions);
        }

        /// <summary>
        /// Client method allowing for the retrieval of all clients 
        /// </summary>
        [HttpGet("/onFirstSession")]
        public async Task<IActionResult> GetClientsOnFirstBlockSession()
        {
            List<string> clientSessions = await unitOfWork.ClientRepository.GetClientsOnFirstSessionAsync();

            if (clientSessions == null) return NotFound("No clients currently on their first block session");
            return Ok(clientSessions);
        }


        /// <summary>
        /// Client method for adding a new Client to the database
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddNewClient(string clientName, int? blockSessions)
        {
            await unitOfWork.ClientRepository.AddNewClientAsync(clientName, blockSessions);
            if (await unitOfWork.Complete()) return Ok($"Client: {clientName} added");

            return BadRequest($"Client {clientName} not added");
        }

        /// <summary>
        /// Client method for removing an existing Client from the database
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> RemoveClient(string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            unitOfWork.ClientRepository.RemoveClient(client);
            if (await unitOfWork.Complete()) return Ok($"Client: {clientName} removed");

            return BadRequest($"Client {clientName} not removed");
        }

    }
}
