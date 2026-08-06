using ClientDashboard_API.Authorization;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using ClientDashboard_API.Interfaces.Repositories;
using ClientDashboard_API.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quartz;
using System.Runtime.CompilerServices;

namespace ClientDashboard_API.Controllers
{
    [Authorize]
    public class ClientController(
        IUnitOfWork unitOfWork,
        IClientBlockTerminationHelper clientBlockTerminator,
        IAuthorizationService authorizationService,
        ICurrentUserAccessor currentUserAccessor
        ) : BaseAPIController
    {

        [Authorize(Roles = "Trainer")]
        [HttpGet("allTrainerClients")]
        public async Task<ActionResult<ApiResponseDto<List<Client>>>> GetTrainerClientsAsync()
        {
            var clients = await unitOfWork.ClientRepository.GetAllTrainerClientDataAsync(currentUserAccessor.GetUserId());
            if (!clients.Any())
            {
                return Ok(new ApiResponseDto<List<Client>> { Data = [], Message = $"No clients found", Success = true });
            }

            return Ok(new ApiResponseDto<List<Client>> { Data = clients, Message = "clients gathered.", Success = true });
        }

        [Authorize(Roles = "Trainer")]
        [HttpGet("getClientById")]
        public async Task<ActionResult<ApiResponseDto<string>>> GetClientByIdAsync([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<int> { Data = 0, Message = $"client with id: {clientId} was not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to retrieve this client", Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = client.FirstName, Message = $"client: {client.FirstName}'s name retrieved seccessfully", Success = true });
        }


        // <summary>
        /// Client method allowing for the retrieval of a client phone number
        /// </summary>
        [Authorize(Roles = "Trainer,Client")]
        [HttpGet("getClientPhoneNumber")]
        public async Task<ActionResult<ApiResponseDto<string>>> GetClientPhoneNumberAsync([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client found when trying to retrieve phone number", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to retrieve this client's phone number", Success = false });
            }

            return Ok(new ApiResponseDto<string> { Data = client.PhoneNumber, Message = $"{client.FirstName}'s phone number returned successfully", Success = true });

        }

        /// <summary>
        /// Client method allowing update all client information
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPut("newClientInformation")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeClientInformationAsync([FromBody] Client updatedClient)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(updatedClient.Id);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client with id {updatedClient.Id} not found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to update this client's information", Success = false });
            }

            unitOfWork.ClientRepository.UpdateClientDetailsAsync(client, updatedClient.FirstName, updatedClient.IsActive,
                updatedClient.CurrentBlockSession, updatedClient.TotalBlockSessions, updatedClient.PhoneNumber);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Failed to update {updatedClient.FirstName}'s details", Success = false });
            }

            if (client.CurrentBlockSession == client.TotalBlockSessions)
            {
                await clientBlockTerminator.CreateAllAdequateEntityReminderAsync(client);
            }
            return Ok(new ApiResponseDto<string> { Data = null, Message = $"{updatedClient.FirstName}'s details have been updated successfully", Success = true });

        }

        /// <summary>
        /// Client method allowing update of client's phone number
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPut("setClientPhoneNumber")]
        public async Task<ActionResult<ApiResponseDto<string>>> ChangeClientPhoneNumberAsync([FromBody] ClientPhoneNumberUpdateDto clientInfo)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientInfo.Id);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client found when trying to assign phone number", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to set this client's phone number", Success = false });
            }

            unitOfWork.ClientRepository.UpdateClientPhoneNumber(client, clientInfo.PhoneNumber);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Failed to update {client.FirstName}'s phone number", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = client.PhoneNumber, Message = $"{client.FirstName}'s phone number haas been updated successfully", Success = true });

        }

        /// <summary>
        /// Client method for removing currently allocated trainer
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPut("unAssignTrainer")]
        public async Task<ActionResult<ApiResponseDto<string>>> UnAssignCurrentTrainerAsync([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"No client with the id {clientId} found", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to unassign from this client", Success = false });
            }

            unitOfWork.ClientRepository.UnassignTrainerAsync(client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Problem occurring while unassigning {client.FirstName}'s trainer", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = null, Message = $"{client.FirstName}'s trainer is now unassigned", Success = true });
        }

        /// <summary>
        /// Client method for adding a new Client to the database via client object body
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpPost("addNewClient")]
        public async Task<ActionResult<ApiResponseDto<string>>> AddNewClientAsync([FromBody] ClientAddDto newClient)
        {
            var trainerId = currentUserAccessor.GetUserId();
            if(trainerId != newClient.TrainerId)
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = "This trainer cannot add this client under themselves", Success = false });
            }

            await unitOfWork.ClientRepository.AddNewClientUnderTrainerAsync(newClient.FirstName, newClient.TotalBlockSessions, newClient.PhoneNumber, newClient.TrainerId);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client {newClient.FirstName} not added", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = newClient.FirstName, Message = $"Client: {newClient.FirstName} added", Success = true });

        }


        /// <summary>
        /// Client method for removing an existing Client from the database via id
        /// </summary>
        [Authorize(Roles = "Trainer")]
        [HttpDelete("removeClientById")]
        public async Task<ActionResult<ApiResponseDto<string>>> RemoveClientByIdAsync([FromQuery] int clientId)
        {
            var client = await unitOfWork.ClientRepository.GetClientByIdAsync(clientId);
            if (client is null)
            {
                return NotFound(new ApiResponseDto<string> { Data = null, Message = $"Client with id: {clientId} not found in the database", Success = false });
            }

            var authResult = await authorizationService.AuthorizeAsync(User, client, new ResourceOwnerRequirement());
            if (!authResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new ApiResponseDto<string> { Data = null, Message = "Not authorized to remove this client", Success = false });
            }

            unitOfWork.ClientRepository.SoftDeleteClientAsync(client);

            if (!await unitOfWork.Complete())
            {
                return BadRequest(new ApiResponseDto<string> { Data = null, Message = $"Client: {client.FirstName} not removed", Success = false });
            }
            return Ok(new ApiResponseDto<string> { Data = clientId.ToString(), Message = $"Client: {client.FirstName} was removed", Success = true });

        }
   
    }
}
