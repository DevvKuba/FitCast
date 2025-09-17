using AutoMapper;
using ClientDashboard_API.Controllers;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
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
        private readonly UnitOfWork _unitOfWork;
        private readonly ClientController _clientController;

        public ClientControllerTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDTO, Client>();
            });
            _mapper = config.CreateMapper();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                // guid means a db will be created for each given test
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            _context = new DataContext(optionsBuilder.Options);
            _clientRepository = new ClientRepository(_context, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _clientRepository, _workoutRepository);
            _clientController = new ClientController(_unitOfWork);
        }

        [Fact]
        public async Task TestCorrectlyGettingCurrentClientBlockSessionAsync()
        {
            // by default adding a new client sets their current session to 0
            await _unitOfWork.ClientRepository.AddNewClientAsync("Rob", 8);
            await _unitOfWork.Complete();

            var currentClientSession = await _clientController.GetCurrentClientBlockSessionAsync("Rob");
            var expectedClientSession = 0;

            Assert.Equal(currentClientSession.Value, expectedClientSession);
        }

        [Fact]
        public async Task TestIncorrectlyGettingCurrentClientBlockSessionAsync()
        {
            // by default adding a new client sets their current session to 0
            await _unitOfWork.ClientRepository.AddNewClientAsync("Rob", 8);
            await _unitOfWork.Complete();

            var currentClientSession = await _clientController.GetCurrentClientBlockSessionAsync("Rob");
            var incorrectClientSession = 1;

            Assert.NotEqual(currentClientSession.Value, incorrectClientSession);
        }

        [Fact]
        public async Task TestCorrectlyGettingClientsOnLastBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { Name = "rob", CurrentBlockSession = 4, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { Name = "mat", CurrentBlockSession = 8, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnLastBlockSessionAsync();
            var okResult = actionResult.Result as ObjectResult;
            var clientSessions = okResult!.Value as List<string> ?? new List<string>();

            Assert.Equal(2, clientSessions.Count());

        }

        [Fact]
        public async Task TestIncorrectlyGettingClientsOnLastBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { Name = "rob", CurrentBlockSession = 3, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { Name = "mat", CurrentBlockSession = 1, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var clientsOnLastSession = await _clientController.GetClientsOnLastBlockSessionAsync();

            Assert.Null(clientsOnLastSession?.Value?[0]);
        }

        [Fact]
        public async Task TestCorrectlyGettingClientsOnFirstBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { Name = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { Name = "mat", CurrentBlockSession = 1, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var actionResult = await _clientController.GetClientsOnFirstBlockSessionAsync();
            var okResult = actionResult.Result as ObjectResult;
            var clientSessions = okResult!.Value as List<string> ?? new List<string>();

            Assert.Equal(2, clientSessions.Count());
        }

        [Fact]
        public async Task TestIncorrectlyGettingClientsOnFirstBlockSessionAsync()
        {
            await _context.Client.AddAsync(new Client { Name = "rob", CurrentBlockSession = 3, TotalBlockSessions = 4 });
            await _context.Client.AddAsync(new Client { Name = "mat", CurrentBlockSession = 5, TotalBlockSessions = 8 });
            await _unitOfWork.Complete();

            var clientsOnFirstSession = await _clientController.GetClientsOnFirstBlockSessionAsync();

            Assert.Null(clientsOnFirstSession?.Value?[0]);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientTotalSessionsAsync()
        {
            var clientName = "rob";
            var currentBlockSession = 1;
            var oldTotalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { Name = clientName, CurrentBlockSession = currentBlockSession, TotalBlockSessions = oldTotalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newTotalSessions = 8;
            await _clientController.ChangeClientTotalSessions(client!.Name, newTotalSessions);

            Assert.Equal(newTotalSessions, client.TotalBlockSessions);
            Assert.Equal(currentBlockSession, client.CurrentBlockSession);
            Assert.Equal(clientName, client.Name);
        }

        [Fact]
        public async Task TestUnsuccessfullyChangingClientTotalSessionsAsync()
        {
            var clientName = "rob";
            var currentBlockSession = 1;
            var oldTotalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { Name = clientName, CurrentBlockSession = currentBlockSession, TotalBlockSessions = oldTotalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newTotalSessions = 8;
            await _clientController.ChangeClientTotalSessions("mat", newTotalSessions);

            Assert.NotEqual(newTotalSessions, client!.TotalBlockSessions);
            Assert.Equal(oldTotalBlockSessions, client.TotalBlockSessions);
            Assert.Equal(currentBlockSession, client.CurrentBlockSession);
            Assert.Equal(clientName, client.Name);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientCurrentSessionAsync()
        {
            var clientName = "rob";
            var oldCurrentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { Name = clientName, CurrentBlockSession = oldCurrentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newCurrentSession = 4;
            await _clientController.ChangeClientTotalSessions(client!.Name, newCurrentSession);

            Assert.Equal(newCurrentSession, client.TotalBlockSessions);
            Assert.Equal(oldCurrentSession, client.CurrentBlockSession);
            Assert.Equal(clientName, client.Name);
        }

        [Fact]
        public async Task TestUnsuccessfullyChangingClientCurrentSessionAsync()
        {
            var clientName = "rob";
            var oldCurrentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { Name = clientName, CurrentBlockSession = oldCurrentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newCurrentSession = 4;
            await _clientController.ChangeClientTotalSessions("mat", newCurrentSession);

            Assert.NotEqual(newCurrentSession, client!.CurrentBlockSession);
            Assert.Equal(oldCurrentSession, client.CurrentBlockSession);
            Assert.Equal(totalBlockSessions, client.TotalBlockSessions);
            Assert.Equal(clientName, client.Name);
        }

        [Fact]
        public async Task TestSuccessfullyChangingClientNameAsync()
        {
            var oldClientName = "rob";
            var currentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { Name = oldClientName, CurrentBlockSession = currentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newClientName = "mat";
            await _clientController.ChangeClientName(client!.Name, newClientName);

            Assert.Equal(newClientName, client.Name);
            Assert.Equal(currentSession, client.CurrentBlockSession);
            Assert.Equal(totalBlockSessions, client.TotalBlockSessions);
        }

        [Fact]
        public async Task TestUnsuccessfullyChangingClientNameAsync()
        {
            var oldClientName = "rob";
            var currentSession = 1;
            var totalBlockSessions = 4;

            await _context.Client.AddAsync(new Client { Name = oldClientName, CurrentBlockSession = currentSession, TotalBlockSessions = totalBlockSessions });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newClientName = "mat";
            await _clientController.ChangeClientName("robert", newClientName);

            Assert.NotEqual(newClientName, client!.Name);
            Assert.Equal(oldClientName, client.Name);
            Assert.Equal(currentSession, client.CurrentBlockSession);
            Assert.Equal(totalBlockSessions, client.TotalBlockSessions);
        }


        [Fact]
        public async Task TestSuccessfullyAddingNewClientAsync()
        {
            var clientName = "rob";
            var blockSessions = 4;

            await _clientController.AddNewClientAsync(clientName, blockSessions);

            var addedClient = await _context.Client.FirstOrDefaultAsync();

            Assert.Equal(addedClient!.Name, clientName);
            Assert.Equal(addedClient!.TotalBlockSessions, blockSessions);
        }

        [Fact]
        public async Task TestUnsuccessfullyAddingNewClientAsync()
        {
            var duplicateClient = new Client { Name = "rob", CurrentBlockSession = 1, TotalBlockSessions = 2, Workouts = [] };

            await _clientController.AddNewClientAsync(duplicateClient.Name, duplicateClient.TotalBlockSessions);
            await _clientController.AddNewClientAsync(duplicateClient.Name, duplicateClient.TotalBlockSessions);

            Assert.Equal(1, _context.Client.Count());
        }

        [Fact]
        public async Task TestSuccessfullyRemovingClientAsync()
        {
            var clientName = "rob";
            var blockSessions = 4;

            await _clientController.AddNewClientAsync(clientName, blockSessions);
            await _clientController.RemoveClientAsync(clientName);

            Assert.Empty(_context.Client);
        }

        [Fact]
        public async Task TestUnsuccessfullyRemovingClientAsync()
        {
            var clientName = "rob";
            var blockSessions = 4;

            await _clientController.AddNewClientAsync(clientName, blockSessions);
            await _clientController.RemoveClientAsync("mat");
            var client = await _context.Client.FirstOrDefaultAsync();

            Assert.NotEmpty(_context.Client);
            Assert.Contains(clientName, client!.Name);
        }

    }
}
