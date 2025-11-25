using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class ClientRepositoryTests
    {
        private readonly IMapper _mapper;
        private readonly DataContext _context;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly UnitOfWork _unitOfWork;

        public ClientRepositoryTests()
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
            _unitOfWork = new UnitOfWork(_context, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository);

        }

        [Fact]
        public async Task TestAddingCorrectClientAsync()
        {
            var testClient = new Client { FirstName = "rob", CurrentBlockSession = 0, TotalBlockSessions = 8 };

            await _clientRepository.AddNewClientAsync(clientName: "Rob", blockSessions: 8, 0);
            await _unitOfWork.Complete();
            var databaseClient = await _context.Client.FirstOrDefaultAsync();

            Assert.Equal(databaseClient!.FirstName, testClient.FirstName);
            Assert.Equal(databaseClient.CurrentBlockSession, testClient.CurrentBlockSession);
            Assert.Equal(databaseClient.TotalBlockSessions, testClient.TotalBlockSessions);
            Assert.Equal(databaseClient.Workouts.Count, testClient.Workouts.Count);
        }

        [Fact]
        public async Task TestRemovingClientCorrectlyAsync()
        {
            await _clientRepository.AddNewClientAsync(clientName: "Rob", blockSessions: 8, 0);
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            _clientRepository.RemoveClient(client!);
            await _unitOfWork.Complete();

            Assert.False(_context.Client.Any());
        }

        [Fact]
        public async Task TestCheckingIfExistingClientExistsAsync()
        {
            await _clientRepository.AddNewClientAsync(clientName: "Rob", blockSessions: 8, 0);
            await _unitOfWork.Complete();
            var clientName = "rob";

            bool clientPresent = await _clientRepository.CheckIfClientExistsAsync(clientName);

            Assert.True(clientPresent);
        }

        [Fact]
        public async Task TestGettingAllClientsOnLastSessionsAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 4, TotalBlockSessions = 4, Workouts = [] });
            await _context.AddAsync(new Client { FirstName = "mark", CurrentBlockSession = 8, TotalBlockSessions = 8, Workouts = [] });
            await _unitOfWork.Complete();

            var clientList = await _clientRepository.GetClientsOnLastSessionAsync();

            Assert.True(_context.Client.Any(x => x.FirstName == "rob"));
            Assert.True(_context.Client.Any(x => x.FirstName == "mark"));
        }

        [Fact]
        public async Task TestGettingAllClientsOnFirstSessionsAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 1, TotalBlockSessions = 4, Workouts = [] });
            await _context.AddAsync(new Client { FirstName = "mark", CurrentBlockSession = 1, TotalBlockSessions = 8, Workouts = [] });
            await _unitOfWork.Complete();

            var clientList = await _clientRepository.GetClientsOnFirstSessionAsync();

            Assert.True(_context.Client.Any(x => x.FirstName == "rob"));
            Assert.True(_context.Client.Any(x => x.FirstName == "mark"));
        }

        [Fact]
        public async Task TestGettingClientsCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var clientSession = await _clientRepository.GetClientsCurrentSessionAsync("rob");
            var expectedSessions = 2;

            Assert.Equal(clientSession, expectedSessions);
        }

        [Fact]
        public async Task TestGettingClientByNameAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _clientRepository.GetClientByNameAsync("rob");
            var databaseClient = await _context.Client.FirstOrDefaultAsync();

            Assert.Equal(client, databaseClient);
        }


        [Fact]
        public async Task TestUpdateAddingClientCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var updatedClientSessions = 3;
            _clientRepository.UpdateAddingClientCurrentSessionAsync(client!);

            Assert.Equal(client!.CurrentBlockSession, updatedClientSessions);
        }

        [Fact]
        public async Task TestUpdateAddingClientCurrentSessionNewBlockAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 4, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var updatedClientSessions = 1;
            _clientRepository.UpdateAddingClientCurrentSessionAsync(client!);

            Assert.Equal(client!.CurrentBlockSession, updatedClientSessions);
        }

        [Fact]
        public async Task TestUpdateDeletingClientCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var updatedClientSessions = 1;
            _clientRepository.UpdateDeletingClientCurrentSession(client!);

            Assert.Equal(client!.CurrentBlockSession, updatedClientSessions);
        }

        [Fact]
        public async Task TestUpdatingClientTotalBlockSessionsAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newBlockSessions = 8;
            _clientRepository.UpdateClientTotalBlockSession(client!, newBlockSessions);
            await _unitOfWork.Complete();

            Assert.Equal(newBlockSessions, client!.TotalBlockSessions);
        }

        [Fact]
        public async Task TestUpdatingClientCurrentSessionAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newCurrentSession = 4;
            _clientRepository.UpdateClientCurrentSession(client!, newCurrentSession);
            await _unitOfWork.Complete();

            Assert.Equal(newCurrentSession, client!.CurrentBlockSession);
        }

        [Fact]
        public async Task TestUpdatingClientNameAsync()
        {
            await _context.AddAsync(new Client { FirstName = "rob", CurrentBlockSession = 2, TotalBlockSessions = 4, Workouts = [] });
            await _unitOfWork.Complete();

            var client = await _context.Client.FirstOrDefaultAsync();
            var newName = "robert";
            _clientRepository.UpdateClientName(client!, newName);
            await _unitOfWork.Complete();

            Assert.Equal(newName, client!.FirstName);
        }
    }
}