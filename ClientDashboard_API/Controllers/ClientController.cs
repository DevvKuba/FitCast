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
        public async Task<ActionResult<int>> GetCurrentClientBlockSessionAsync(string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"{clientName} was not found");

            return Ok(client.CurrentBlockSession);
        }


        /// <summary>
        /// Client method allowing for the retrieval of all clients, on their last block session
        /// </summary>
        [HttpGet("/onLastSession")]
        public async Task<ActionResult<List<string>>> GetClientsOnLastBlockSessionAsync()
        {
            var clientSessions = await unitOfWork.ClientRepository.GetClientsOnLastSessionAsync();

            if (clientSessions == null) return NotFound("No clients currently on their last block session");
            return Ok(clientSessions);
        }

        /// <summary>
        /// Client method allowing for the retrieval of all clients 
        /// </summary>
        [HttpGet("/onFirstSession")]
        public async Task<ActionResult<List<string>>> GetClientsOnFirstBlockSessionAsync()
        {
            var clientSessions = await unitOfWork.ClientRepository.GetClientsOnFirstSessionAsync();

            if (clientSessions == null) return NotFound("No clients currently on their first block session");
            return Ok(clientSessions);
        }

        /// <summary>
        /// Client method allowing update of ones total sessions
        /// </summary>
        [HttpPut("{clientName}/{totalSessions}/newTotalSessions")]
        public async Task<IActionResult> ChangeClientTotalSessions(string clientName, int totalSessions)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"No client with the name {clientName} found");

            unitOfWork.ClientRepository.UpdateClientTotalBlockSession(client, totalSessions);
            if (await unitOfWork.Complete()) return Ok($"{clientName}'s total block sessions have now been updated to {totalSessions}");

            return BadRequest($"Problem occuring while saving {clientName}'s new total block sessions");
        }

        /// <summary>
        /// Client method allowing update of ones current session
        /// </summary>
        [HttpPut("{clientName}/{currentSession}/newCurrentSession")]
        public async Task<IActionResult> ChangeClientCurrentSession(string clientName, int currentSession)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"No client with the name {clientName} found");

            unitOfWork.ClientRepository.UpdateClientCurrentSession(client, currentSession);
            if (await unitOfWork.Complete()) return Ok($"{clientName}'s current session has now been updated to {currentSession}");

            return BadRequest($"Problem occuring while saving {clientName}'s new current session");
        }


        /// <summary>
        /// Client method for adding a new Client to the database
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddNewClientAsync(string clientName, int? blockSessions)
        {
            var clientExists = await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName);

            if (clientExists) return BadRequest($"Client {clientName} already exists in the database");

            await unitOfWork.ClientRepository.AddNewClientAsync(clientName, blockSessions);
            if (await unitOfWork.Complete()) return Ok($"Client: {clientName} added");

            return BadRequest($"Client {clientName} not added");
        }

        /// <summary>
        /// Client method for removing an existing Client from the database
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> RemoveClientAsync(string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
            if (client == null) return NotFound($"Client {clientName} not found in the database");

            unitOfWork.ClientRepository.RemoveClient(client);
            if (await unitOfWork.Complete()) return Ok($"Client: {clientName} removed");

            return BadRequest($"Client {clientName} not removed");
        }

    }
}
