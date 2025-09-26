using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ClientDashboard_API.Controllers
{
    public class ClientController(IUnitOfWork unitOfWork) : BaseAPIController
    {

        [HttpGet("/allClients")]
        public async Task<ActionResult<ApiResponseDto<List<Client>>>> GetAllClientsAsync()
        {
            var clients = await unitOfWork.ClientRepository.GetAllClientDataAsync();
            if (!clients.Any())
            {
                return NotFound(new ApiResponseDto<List<Client>> { Data = [], Message = $"No clients found", Success = false });
            }

            return Ok(new ApiResponseDto<List<Client>> { Data = clients, Message = "clients gathered.", Success = true });
        }

        /// <summary>
        /// Client method allowing for the retrieval of the clients current session,
        /// within their respective block
        /// </summary>
        [HttpGet("{clientName}/currentSession")]
        public async Task<ActionResult<ApiResponseDto<int>>> GetCurrentClientBlockSessionAsync(string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<int> { Data = 0, Message = $"{clientName} was not found", Success = false });
            }

            return Ok(new ApiResponseDto<int> { Data = client.CurrentBlockSession, Message = $"Current session for {clientName} retrieved successfully", Success = true });
        }

        /// <summary>
        /// Client method allowing for the retrieval of all clients, on their last block session
        /// </summary>
        [HttpGet("/onLastSession")]
        public async Task<ActionResult<ApiResponseDto<List<string>>>> GetClientsOnLastBlockSessionAsync()
        {
            var clientSessions = await unitOfWork.ClientRepository.GetClientsOnLastSessionAsync();
            if (clientSessions == null || !clientSessions.Any())
            {
                return NotFound(new ApiResponseDto<List<string>> { Data = [], Message = "No clients currently on their last block session", Success = false });
            }

            return Ok(new ApiResponseDto<List<string>> { Data = clientSessions, Message = "Clients on last session retrieved successfully", Success = true });
        }

        /// <summary>
        /// Client method allowing for the retrieval of all clients 
        /// </summary>
        [HttpGet("/onFirstSession")]
        public async Task<ActionResult<ApiResponseDto<List<string>>>> GetClientsOnFirstBlockSessionAsync()
        {
            var clientSessions = await unitOfWork.ClientRepository.GetClientsOnFirstSessionAsync();
            if (clientSessions == null || !clientSessions.Any())
            {
                return NotFound(new ApiResponseDto<List<string>> { Data = [], Message = "No clients currently on their first block session", Success = false });
            }

            return Ok(new ApiResponseDto<List<string>> { Data = clientSessions, Message = "Clients on first session retrieved successfully", Success = true });
        }

        /// <summary>
        /// Client method allowing update all client information
        /// </summary>
        [HttpPut("/newClientInformation")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeClientInformationAsync([FromBody] Client updatedClient)
        {
            var oldClient = await unitOfWork.ClientRepository.GetClientByIdAsync(updatedClient.Id);
            if (oldClient == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client with id {updatedClient.Id} not found", Success = false });
            }

            unitOfWork.ClientRepository.UpdateClientDetailsAsync(oldClient, updatedClient.Name, updatedClient.CurrentBlockSession, updatedClient.TotalBlockSessions);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Failed to update {updatedClient.Name}'s details", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = updatedClient.Name, Message = $"{updatedClient.Name}'s details have been updated successfully", Success = true });

        }

        /// <summary>
        /// Client method allowing update of ones total sessions
        /// </summary>
        [HttpPut("{clientName}/{totalSessions}/newTotalSessions")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeClientTotalSessionsAsync(string clientName, int totalSessions)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client with the name {clientName} found", Success = false });
            }

            unitOfWork.ClientRepository.UpdateClientTotalBlockSession(client, totalSessions);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Problem occurring while saving {clientName}'s new total block sessions", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"{clientName}'s total block sessions have now been updated to {totalSessions}", Success = true });

        }

        /// <summary>
        /// Client method allowing update of ones current session
        /// </summary>
        [HttpPut("{clientName}/{currentSession}/newCurrentSession")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeClientCurrentSessionAsync(string clientName, int currentSession)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client with the name {clientName} found", Success = false });
            }
            unitOfWork.ClientRepository.UpdateClientCurrentSession(client, currentSession);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Problem occurring while saving {clientName}'s new current session", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"{clientName}'s current session has now been updated to {currentSession}", Success = true });

        }

        /// <summary>
        /// Client method allowing update of client's given name
        /// </summary>
        [HttpPut("{currentName}/{newName}/newClientName")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeClientNameAsync(string currentName, string newName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(currentName);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client with the name {currentName} found", Success = false });
            }

            unitOfWork.ClientRepository.UpdateClientName(client, newName);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Problem occurring while saving {currentName}'s new name", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = newName, Message = $"{currentName}'s name is now updated to {newName}", Success = true });

        }

        /// <summary>
        /// Client method for adding a new Client to the database via client params
        /// </summary>
        [HttpPost("/ByParams")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewClientAsync([FromQuery] string clientName, [FromQuery] int? blockSessions)
        {
            var clientExists = await unitOfWork.ClientRepository.CheckIfClientExistsAsync(clientName);
            if (clientExists)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client {clientName} already exists in the database", Success = false });
            }

            await unitOfWork.ClientRepository.AddNewClientAsync(clientName, blockSessions);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client {clientName} not added", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"Client: {clientName} added", Success = true });

        }

        /// <summary>
        /// Client method for adding a new Client to the database via client object body
        /// </summary>
        [HttpPost("/ByBody")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewClientObjectAsync([FromBody] Client client)
        {
            var clientExists = await unitOfWork.ClientRepository.GetClientByIdAsync(client.Id);
            if (clientExists != null)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client {client.Name} already exists in the database", Success = false });
            }

            await unitOfWork.ClientRepository.AddNewClientAsync(client.Name, client.TotalBlockSessions);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client {client.Name} not added", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = client.Name, Message = $"Client: {client.Name} added", Success = true });

        }

        /// <summary>
        /// Client method for removing an existing Client from the database via name
        /// </summary>
        [HttpDelete("/ByName")]
        public async Task<ActionResult<ApiResponseDto<string>>> RemoveClientAsync([FromQuery] string clientName)
        {
            var client = await unitOfWork.ClientRepository.GetClientByNameAsync(clientName);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client {clientName} not found in the database", Success = false });
            }

            unitOfWork.ClientRepository.RemoveClient(client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client {clientName} not removed", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = clientName, Message = $"Client: {clientName} removed", Success = true });

        }

        /// <summary>
        /// Client method for removing an existing Client from the database via id
        /// </summary>
        [HttpDelete("/ById")]
        public async Task<ActionResult<ApiResponseDto<string>>> RemoveClientByIdAsync([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);
            if (client == null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client with id: {clientId} not found in the database", Success = false });
            }

            unitOfWork.ClientRepository.RemoveClient(client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client with id: {clientId} not removed", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = clientId.ToString(), Message = $"Client with id: {clientId} removed", Success = true });

        }
    }
}
