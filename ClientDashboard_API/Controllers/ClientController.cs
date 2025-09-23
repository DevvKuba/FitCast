using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientController(IUnitOfWork unitOfWork) : BaseAPIController
    {

        [HttpGet("/allClients")]
        public async Task<ActionResult<List<Client>>> GetAllClientsAsync()
        {
            var clients = await unitOfWork.ClientRepository.GetAllClientDataAsync();

            if (!clients.Any()) return NotFound($"No clients found");

            return Ok(clients);
        }
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
        /// Client method allowing update all client information
        /// </summary>
        [HttpPut("/newClientInformation")]
        public async Task<IActionResult> ChangeClientInformationAsync([FromBody] Client updatedClient)
        {
            var oldClient = await unitOfWork.ClientRepository.GetClientByIdAsync(updatedClient.Id);

            unitOfWork.ClientRepository.UpdateClientDetailsAsync(oldClient, updatedClient.Name, updatedClient.CurrentBlockSession, updatedClient.TotalBlockSessions);
            if (await unitOfWork.Complete()) return Ok($"{updatedClient.Name}'s details have been updated successfuly");
            return BadRequest($"Failed to update {updatedClient.Name}'s details");
        }

        /// <summary>
        /// Client method allowing update of ones total sessions
        /// </summary>
        [HttpPut("{clientName}/{totalSessions}/newTotalSessions")]
        public async Task<IActionResult> ChangeClientTotalSessionsAsync(string clientName, int totalSessions)
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
        public async Task<IActionResult> ChangeClientCurrentSessionAsync(string clientName, int currentSession)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);

            if (client == null) return NotFound($"No client with the name {clientName} found");

            unitOfWork.ClientRepository.UpdateClientCurrentSession(client, currentSession);
            if (await unitOfWork.Complete()) return Ok($"{clientName}'s current session has now been updated to {currentSession}");

            return BadRequest($"Problem occuring while saving {clientName}'s new current session");
        }

        /// <summary>
        /// Client method allowing update of client's given name
        /// </summary>
        [HttpPut("{currentName}/{newName}/newClientName")]
        public async Task<IActionResult> ChangeClientNameAsync(string currentName, string newName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(currentName);

            if (client == null) return NotFound($"No client with the name {currentName} found");

            unitOfWork.ClientRepository.UpdateClientName(client, newName);
            if (await unitOfWork.Complete()) return Ok($"{client.Name}'s name is now updated to {newName}");

            return BadRequest($"Problem occuring while saving {client.Name}'s new name");
        }


        /// <summary>
        /// Client method for adding a new Client to the database via client params
        /// </summary>
        [HttpPost("/ByParams")]
        public async Task<IActionResult> AddNewClientAsync([FromQuery] string clientName, [FromQuery] int? blockSessions)
        {
            var clientExists = await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName);

            if (clientExists) return BadRequest($"Client {clientName} already exists in the database");

            await unitOfWork.ClientRepository.AddNewClientAsync(clientName, blockSessions);
            if (await unitOfWork.Complete()) return Ok($"Client: {clientName} added");

            return BadRequest($"Client {clientName} not added");
        }

        /// <summary>
        /// Client method for adding a new Client to the database via client object body
        /// </summary>
        [HttpPost("/ByBody")]
        public async Task<IActionResult> AddNewClientObjectAsync([FromBody] Client client)
        {
            var clientExists = await unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);

            if (clientExists != null) return BadRequest($"Client {client.Name} already exists in the database");

            await unitOfWork.ClientRepository.AddNewClientAsync(client.Name, client.TotalBlockSessions);
            if (await unitOfWork.Complete()) return Ok(new { message = "$Client: {client.Name} added", success = true });

            return BadRequest(new { message = $"Client {client.Name} not added", success = false });
        }

        /// <summary>
        /// Client method for removing an existing Client from the database via name
        /// </summary>
        [HttpDelete("/ByName")]
        // eventually change to by Name
        public async Task<IActionResult> RemoveClientAsync([FromQuery] string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
            if (client == null) return NotFound($"Client {clientName} not found in the database");

            unitOfWork.ClientRepository.RemoveClient(client);
            if (await unitOfWork.Complete()) return Ok(new { message = $"Client: {clientName} removed", success = true });

            return BadRequest(new { message = $"Client {clientName} not removed", success = false });
        }

        /// <summary>
        /// Client method for removing an existing Client from the database via id
        /// </summary>
        [HttpDelete("/ById")]
        // eventually change to by Name
        public async Task<IActionResult> RemoveClientByIdAsync([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);
            if (client == null) return NotFound($"Client with id: {clientId} not found in the database");

            unitOfWork.ClientRepository.RemoveClient(client);
            if (await unitOfWork.Complete()) return Ok($"Client with id: {clientId} removed");

            return BadRequest($"Client with id: {clientId} not removed");
        }

    }
}
