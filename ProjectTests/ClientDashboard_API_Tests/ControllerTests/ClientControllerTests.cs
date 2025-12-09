using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.ControllerTests
{
    public class ClientControllerTests
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;
        private readonly ClientDailyFeatureService _clientDailyFeatureService;
        private readonly ClientController _clientController;

        public ClientControllerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
            });
            _mapper = config.CreateMapper();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                // guid means a db will be created for each given test
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            _context = new DataContext(optionsBuilder.Options);
            _clientRepository = new ClientRepository(_context, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository);
            _clientController = new ClientController(_unitOfWork, _clientDailyFeatureService);
        }

        [Fact]
        public async Task TestCorrectlyGettingCurrentClientBlockSessionAsync()
        {
            // by default adding a new client sets their current session to 0
            await _unitOfWork.ClientRepository.AddNewClientUnderTrainerAsync("Rob", 8, null, null);
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetCurrentClientBlockSessionAsync("Rob");
            var objectResult = actionResult.Result as ObjectResult;
            var currentClientSession = objectResult!.Value as ApiResponseDto<int> ?? new ApiResponseDto<int> { Message = "", Success = false };
            var expectedClientSession = 0;

            Assert.Equal(currentClientSession!.Data, expectedClientSession);
        }

        [Fact]
        public async Task TestIncorrectlyGettingCurrentClientBlockSessionAsync()
        {
            // by default adding a new client sets their current session to 0
            await _unitOfWork.ClientRepository.AddNewClientUnderTrainerAsync("Rob", 8, null, null);
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetCurrentClientBlockSessionAsync("Rob");
            var okResult = actionResult.Result as ObjectResult;
            var currentClientSession = okResult!.Value as ApiResponseDto<int> ?? new ApiResponseDto<int> { Message = "", Success = false };
            var incorrectClientSession = 1;

            Assert.NotEqual(currentClientSession!.Data, incorrectClientSession);
        }

        [Fact]
        public async Task TestCorrectlyGettingClientsOnLastBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 4, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { FirstName = "mat", CurrentBlockSession = 8, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnLastBlockSessionAsync();
            var okResult = actionResult.Result as ObjectResult;
            var clientSessions = okResult!.Value as ApiResponseDto<List<string>> ?? new ApiResponseDto<List<string>> { Data = [], Message = "", Success = false };

            Assert.Equal(2, clientSessions!.Data!.Count());

        }

        [Fact]
        public async Task TestIncorrectlyGettingClientsOnLastBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 3, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { FirstName = "mat", CurrentBlockSession = 1, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnLastBlockSessionAsync();
            var okResult = actionResult.Result as ObjectResult;
            var clientsOnLastSession = okResult!.Value as ApiResponseDto<List<string>> ?? new ApiResponseDto<List<string>> { Data = [], Message = "", Success = false };

            Assert.Equal(clientsOnLastSession?.Data!.Count, 0);
        }

        [Fact]
        public async Task TestCorrectlyGettingClientsOnFirstBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { FirstName = "mat", CurrentBlockSession = 1, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnFirstBlockSessionAsync();
            var okResult = actionResult.Result as ObjectResult;
            var clientSessions = okResult!.Value as ApiResponseDto<List<string>> ?? new ApiResponseDto<List<string>> { Data = [], Message = "", Success = false };

            Assert.Equal(2, clientSessions!.Data!.Count());
        }

        [Fact]
        public async Task TestIncorrectlyGettingClientsOnFirstBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 3, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { FirstName = "mat", CurrentBlockSession = 5, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnFirstBlockSessionAsync();
            var okResult = actionResult.Result as ObjectResult;
            var clientsOnFirstSession = okResult!.Value as ApiResponseDto<List<string>> ?? new ApiResponseDto<List<string>> { Data = [], Message = "", Success = false };

            Assert.Equal(clientsOnFirstSession?.Data!.Count, 0);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientTotalSessionsAsync()
        {
            var clientName = "rob";
            var currentBlockSession = 1;
            var oldTotalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = currentBlockSession, TotalBlockSessions = oldTotalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newTotalSessions = 8;
            await _clientController.ChangeClientTotalSessionsAsync(client!.FirstName, newTotalSessions);

            Assert.Equal(newTotalSessions, client.TotalBlockSessions);
            Assert.Equal(currentBlockSession, client.CurrentBlockSession);
            Assert.Equal(clientName, client.FirstName);
        }

        [Fact]
        public async Task TestUnsuccessfullyChangingClientTotalSessionsAsync()
        {
            var clientName = "rob";
            var currentBlockSession = 1;
            var oldTotalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = currentBlockSession, TotalBlockSessions = oldTotalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newTotalSessions = 8;
            await _clientController.ChangeClientTotalSessionsAsync("mat", newTotalSessions);

            Assert.NotEqual(newTotalSessions, client!.TotalBlockSessions);
            Assert.Equal(oldTotalBlockSessions, client.TotalBlockSessions);
            Assert.Equal(currentBlockSession, client.CurrentBlockSession);
            Assert.Equal(clientName, client.FirstName);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientCurrentSessionAsync()
        {
            var clientName = "rob";
            var oldCurrentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = oldCurrentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newCurrentSession = 4;
            await _clientController.ChangeClientTotalSessionsAsync(client!.FirstName, newCurrentSession);

            Assert.Equal(newCurrentSession, client.TotalBlockSessions);
            Assert.Equal(oldCurrentSession, client.CurrentBlockSession);
            Assert.Equal(clientName, client.FirstName);
        }

        [Fact]
        public async Task TestUnsuccessfullyChangingClientCurrentSessionAsync()
        {
            var clientName = "rob";
            var oldCurrentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { FirstName = clientName, CurrentBlockSession = oldCurrentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newCurrentSession = 4;
            await _clientController.ChangeClientTotalSessionsAsync("mat", newCurrentSession);

            Assert.NotEqual(newCurrentSession, client!.CurrentBlockSession);
            Assert.Equal(oldCurrentSession, client.CurrentBlockSession);
            Assert.Equal(totalBlockSessions, client.TotalBlockSessions);
            Assert.Equal(clientName, client.FirstName);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientNameAsync()
        {
            var oldClientName = "rob";
            var currentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { FirstName = oldClientName, CurrentBlockSession = currentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newClientName = "mat";
            await _clientController.ChangeClientNameAsync(client!.FirstName, newClientName);

            Assert.Equal(newClientName, client.FirstName);
            Assert.Equal(currentSession, client.CurrentBlockSession);
            Assert.Equal(totalBlockSessions, client.TotalBlockSessions);
        }

        [Fact]
        public async Task TestUnsuccessfullyChangingClientNameAsync()
        {
            var oldClientName = "rob";
            var currentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { FirstName = oldClientName, CurrentBlockSession = currentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newClientName = "mat";
            await _clientController.ChangeClientNameAsync("robert", newClientName);

            Assert.NotEqual(newClientName, client!.FirstName);
            Assert.Equal(oldClientName, client.FirstName);
            Assert.Equal(currentSession, client.CurrentBlockSession);
            Assert.Equal(totalBlockSessions, client.TotalBlockSessions);
        }


        [Fact]
        public async Task TestSuccessfullyAddingNewClientAsync()
        {
            var clientName = "rob";
            var blockSessions = 4;

            await _clientController.AddNewClientAsync(clientName, blockSessions, phoneNumber: "", trainerId: 1);

            var addedClient = await _context.Client.FirstOrDefaultAsync();

            Assert.Equal(addedClient!.FirstName, clientName);
            Assert.Equal(addedClient!.TotalBlockSessions, blockSessions);
        }

        [Fact]
        public async Task TestUnsuccessfullyAddingNewClientAsync()
        {
            var duplicateClient = new Client { FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 2, Workouts = [] };

            await _clientController.AddNewClientAsync(duplicateClient.FirstName, duplicateClient.TotalBlockSessions, phoneNumber: "", trainerId: 1);
            await _clientController.AddNewClientAsync(duplicateClient.FirstName, duplicateClient.TotalBlockSessions, phoneNumber: "", trainerId: 1);

            Assert.Equal(1, _context.Client.Count());
        }

        [Fact]
        public async Task TestSuccessfullyRemovingClientAsync()
        {
            var clientName = "rob";
            var blockSessions = 4;

            await _clientController.AddNewClientAsync(clientName, blockSessions, phoneNumber: "", trainerId: 1);
            await _clientController.RemoveClientAsync(clientName);

            Assert.Empty(_context.Client);
        }

        [Fact]
        public async Task TestUnsuccessfullyRemovingClientAsync()
        {
            var clientName = "rob";
            var blockSessions = 4;

            await _clientController.AddNewClientAsync(clientName, blockSessions, phoneNumber: "", trainerId: 1);
            await _clientController.RemoveClientAsync("mat");
            var client = await _context.Client.FirstOrDefaultAsync();

            Assert.NotEmpty(_context.Client);
            Assert.Contains(clientName, client!.FirstName);
        }

    }
}
