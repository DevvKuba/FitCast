using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class TrainerRepositoryTests
    {
        private readonly IMapper _mapper;
        private readonly IPasswordHasher _passwordHasher;
        private readonly DataContext _context;
        private readonly UserRepository _userRepository;
        private readonly ClientRepository _clientRepository;
        private readonly WorkoutRepository _workoutRepository;
        private readonly TrainerRepository _trainerRepository;
        private readonly NotificationRepository _notificationRepository;
        private readonly PaymentRepository _paymentRepository;
        private readonly EmailVerificationTokenRepository _emailVerificationTokenRepository;
        private readonly PasswordResetTokenRepository _passwordResetTokenRepository;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;

        public TrainerRepositoryTests()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Client, WorkoutDto>();
                cfg.CreateMap<ClientUpdateDto, Client>();
                cfg.CreateMap<PaymentUpdateDto, Payment>();
                cfg.CreateMap<TrainerUpdateDto, Trainer>();
            });
            _mapper = config.CreateMapper();
            _passwordHasher = new PasswordHasher();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            _context = new DataContext(optionsBuilder.Options);
            _userRepository = new UserRepository(_context, _passwordHasher);
            _clientRepository = new ClientRepository(_context, _passwordHasher, _mapper);
            _workoutRepository = new WorkoutRepository(_context);
            _trainerRepository = new TrainerRepository(_context, _mapper);
            _notificationRepository = new NotificationRepository(_context);
            _paymentRepository = new PaymentRepository(_context, _mapper);
            _emailVerificationTokenRepository = new EmailVerificationTokenRepository(_context);
            _passwordResetTokenRepository = new PasswordResetTokenRepository(_context);
            _clientDailyFeatureRepository = new ClientDailyFeatureRepository(_context);
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);
        }

        [Fact]
        public async Task TestGetTrainerByEmailAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Email = "john@example.com", 
                Role = UserRole.Trainer 
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var retrievedTrainer = await _trainerRepository.GetTrainerByEmailAsync("john@example.com");

            Assert.NotNull(retrievedTrainer);
            Assert.Equal("john@example.com", retrievedTrainer.Email);
            Assert.Equal("john", retrievedTrainer.FirstName);
        }

        [Fact]
        public async Task TestGetTrainerByEmailReturnsNullForNonExistentEmailAsync()
        {
            var trainer = await _trainerRepository.GetTrainerByEmailAsync("nonexistent@example.com");

            Assert.Null(trainer);
        }

        [Fact]
        public async Task TestGetTrainerByPhoneNumberAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                PhoneNumber = "1234567890", 
                Role = UserRole.Trainer 
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var retrievedTrainer = await _trainerRepository.GetTrainerByPhoneNumberAsync("123 456 7890");

            Assert.NotNull(retrievedTrainer);
            Assert.Equal("1234567890", retrievedTrainer.PhoneNumber);
            Assert.Equal("john", retrievedTrainer.FirstName);
        }

        [Fact]
        public async Task TestGetTrainerByPhoneNumberHandlesSpacesAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                PhoneNumber = "1234567890", 
                Role = UserRole.Trainer 
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var retrievedTrainer = await _trainerRepository.GetTrainerByPhoneNumberAsync("123 456 7890");

            Assert.NotNull(retrievedTrainer);
            Assert.Equal("1234567890", retrievedTrainer.PhoneNumber);
        }

        [Fact]
        public async Task TestGetTrainerByIdAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer 
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var retrievedTrainer = await _trainerRepository.GetTrainerByIdAsync(trainer.Id);

            Assert.NotNull(retrievedTrainer);
            Assert.Equal(trainer.Id, retrievedTrainer.Id);
            Assert.Equal("john", retrievedTrainer.FirstName);
        }

        [Fact]
        public async Task TestGetTrainerByIdReturnsNullForNonExistentIdAsync()
        {
            var trainer = await _trainerRepository.GetTrainerByIdAsync(999);

            Assert.Null(trainer);
        }

        [Fact]
        public async Task TestGetAllTrainersAsync()
        {
            await _context.Trainer.AddAsync(new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer });
            await _context.Trainer.AddAsync(new Trainer { FirstName = "jane", Surname = "smith", Role = UserRole.Trainer });
            await _unitOfWork.Complete();

            var trainers = await _trainerRepository.GetAllTrainersAsync();

            Assert.Equal(2, trainers.Count);
            Assert.Contains(trainers, t => t.FirstName == "john");
            Assert.Contains(trainers, t => t.FirstName == "jane");
        }

        [Fact]
        public async Task TestGetAllTrainersEligibleForRevenueTrackingAsync()
        {
            var oldTrainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Role = UserRole.Trainer,
                Clients =
                [
                    new Client
                    {
                        FirstName = "rob",
                        Role = UserRole.Client,
                        CurrentBlockSession = 1,
                        TotalBlockSessions = 4,
                        Workouts = [
                                new  Workout {
                                    ClientName = "rob",
                                    WorkoutTitle = "Test",
                                    SessionDate = DateOnly.FromDateTime(DateTime.UtcNow) }
                            ]
                    }]
            };

            var newTrainer = new Trainer 
            { 
                FirstName = "jane", 
                Surname = "smith", 
                Role = UserRole.Trainer,
            };

            await _context.Trainer.AddAsync(oldTrainer);
            await _context.Trainer.AddAsync(newTrainer);
            await _unitOfWork.Complete();

            _context.Entry(oldTrainer).Property("CreatedAt").CurrentValue = DateTime.UtcNow.AddDays(-20);
            _context.Entry(newTrainer).Property("CreatedAt").CurrentValue = DateTime.UtcNow.AddDays(-5);
            await _unitOfWork.Complete();

            var eligibleTrainers = await _trainerRepository.GetAllTrainersEligibleForRevenueTrackingAsync();

            Assert.Single(eligibleTrainers);
            Assert.Equal("john", eligibleTrainers[0].FirstName);
            Assert.NotEmpty(eligibleTrainers[0].Clients);
            Assert.NotEmpty(eligibleTrainers[0].Clients[0].Workouts);
        }

        [Fact]
        public async Task TestGetTrainerWithClientsByIdAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer,
                Clients = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.Client.AddAsync(new Client 
            { 
                FirstName = "rob", 
                Role = UserRole.Client, 
                TrainerId = trainer.Id,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4,
                Workouts = []
            });
            await _context.Client.AddAsync(new Client 
            { 
                FirstName = "mark", 
                Role = UserRole.Client, 
                TrainerId = trainer.Id,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 8,
                Workouts = []
            });
            await _unitOfWork.Complete();

            var retrievedTrainer = await _trainerRepository.GetTrainerWithClientsByIdAsync(trainer.Id);

            Assert.NotNull(retrievedTrainer);
            Assert.Equal(2, retrievedTrainer.Clients.Count);
            Assert.Contains(retrievedTrainer.Clients, c => c.FirstName == "rob");
            Assert.Contains(retrievedTrainer.Clients, c => c.FirstName == "mark");
        }

        [Fact]
        public async Task TestGetTrainersWithAutoRetrievalAsync()
        {
            await _context.Trainer.AddAsync(new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer,
                AutoWorkoutRetrieval = true
            });
            await _context.Trainer.AddAsync(new Trainer 
            { 
                FirstName = "jane", 
                Surname = "smith", 
                Role = UserRole.Trainer,
                AutoWorkoutRetrieval = false
            });
            await _context.Trainer.AddAsync(new Trainer 
            { 
                FirstName = "bob", 
                Surname = "jones", 
                Role = UserRole.Trainer,
                AutoWorkoutRetrieval = true
            });
            await _unitOfWork.Complete();

            var trainers = await _trainerRepository.GetTrainersWithAutoRetrievalAsync();

            Assert.Equal(2, trainers.Count);
            Assert.All(trainers, t => Assert.True(t.AutoWorkoutRetrieval));
            Assert.Contains(trainers, t => t.FirstName == "john");
            Assert.Contains(trainers, t => t.FirstName == "bob");
        }

        [Fact]
        public async Task TestGetTrainerClientsWithWorkoutsAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var client1 = new Client 
            { 
                FirstName = "rob", 
                Role = UserRole.Client, 
                TrainerId = trainer.Id,
                Trainer = trainer,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4,
                Workouts = []
            };
            var client2 = new Client 
            { 
                FirstName = "mark", 
                Role = UserRole.Client, 
                TrainerId = trainer.Id,
                Trainer = trainer,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 8,
                Workouts = []
            };
            await _context.Client.AddAsync(client1);
            await _context.Client.AddAsync(client2);
            await _unitOfWork.Complete();

            await _context.Workouts.AddAsync(new Workout 
            { 
                ClientName = client1.FirstName,
                ClientId = client1.Id, 
                WorkoutTitle = "Workout 1",
                SessionDate = DateOnly.FromDateTime(DateTime.UtcNow)
            });
            await _unitOfWork.Complete();

            var clients = await _trainerRepository.GetTrainerClientsWithWorkoutsAsync(trainer);

            Assert.Equal(2, clients.Count);
            Assert.NotNull(clients[0].Workouts);
            Assert.Contains(clients, c => c.FirstName == "rob");
        }

        [Fact]
        public async Task TestGetTrainerActiveClientsAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _context.Client.AddAsync(new Client 
            { 
                FirstName = "rob", 
                Role = UserRole.Client, 
                TrainerId = trainer.Id,
                Trainer = trainer,
                IsActive = true,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4,
                Workouts = []
            });
            await _context.Client.AddAsync(new Client 
            { 
                FirstName = "mark", 
                Role = UserRole.Client, 
                TrainerId = trainer.Id,
                Trainer = trainer,
                IsActive = false,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 8,
                Workouts = []
            });
            await _unitOfWork.Complete();

            var activeClients = await _trainerRepository.GetTrainerActiveClientsAsync(trainer);

            Assert.Single(activeClients);
            Assert.Equal("rob", activeClients[0].FirstName);
            Assert.True(activeClients[0].IsActive);
        }

        [Fact]
        public async Task TestAssignClientAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer
            };
            var client = new Client 
            { 
                FirstName = "rob", 
                Role = UserRole.Client,
                CurrentBlockSession = 1, 
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            _trainerRepository.AssignClient(trainer, client);
            await _unitOfWork.Complete();

            Assert.Equal(trainer.Id, client.TrainerId);
        }

        [Fact]
        public async Task TestUpdateTrainerProfileDetailsAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Email = "john@example.com",
                Role = UserRole.Trainer,
                BusinessName = "Old Business"
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var updateDto = new TrainerUpdateDto
            {
                FirstName = "jonathan",
                Surname = "doe jr",
                Email = "jonathan@example.com",
                PhoneNumber = "1234567890",
                BusinessName = "New Business",
                DefaultCurrency = "£",
                AverageSessionPrice = 50.00m
            };

            _trainerRepository.UpdateTrainerProfileDetailsAsync(trainer, updateDto);
            await _unitOfWork.Complete();

            Assert.Equal("jonathan", trainer.FirstName);
            Assert.Equal("doe jr", trainer.Surname);
            Assert.Equal("jonathan@example.com", trainer.Email);
            Assert.Equal("1234567890", trainer.PhoneNumber);
            Assert.Equal("New Business", trainer.BusinessName);
            Assert.Equal("£", trainer.DefaultCurrency);
            Assert.Equal(50.00m, trainer.AverageSessionPrice);
        }

        [Fact]
        public async Task TestUpdateTrainerPhoneNumberAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                PhoneNumber = "1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await _trainerRepository.UpdateTrainerPhoneNumberAsync(trainer.Id, "9876543210");
            await _unitOfWork.Complete();

            var updatedTrainer = await _context.Trainer.FirstOrDefaultAsync(t => t.Id == trainer.Id);
            Assert.Equal("9876543210", updatedTrainer!.PhoneNumber);
        }

        [Fact]
        public async Task TestUpdateTrainerAutoRetrievalAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer,
                AutoWorkoutRetrieval = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _trainerRepository.UpdateTrainerAutoRetrievalAsync(trainer, true);
            await _unitOfWork.Complete();

            Assert.True(trainer.AutoWorkoutRetrieval);
        }

        [Fact]
        public async Task TestUpdateTrainerPaymentSettingAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer,
                AutoPaymentSetting = false
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _trainerRepository.UpdateTrainerPaymentSettingAsync(trainer, true);
            await _unitOfWork.Complete();

            Assert.True(trainer.AutoPaymentSetting);
        }

        [Fact]
        public async Task TestUpdateTrainerApiKeyAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer,
                WorkoutRetrievalApiKey = null
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var newApiKey = "test-api-key-12345";
            _trainerRepository.UpdateTrainerApiKeyAsync(trainer, newApiKey);
            await _unitOfWork.Complete();

            Assert.Equal(newApiKey, trainer.WorkoutRetrievalApiKey);
        }

        [Fact]
        public async Task TestAddNewTrainerAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };

            await _trainerRepository.AddNewTrainerAsync(trainer);
            await _unitOfWork.Complete();

            var savedTrainer = await _context.Trainer.FirstOrDefaultAsync(t => t.Email == "john@example.com");
            Assert.NotNull(savedTrainer);
            Assert.Equal("john", savedTrainer.FirstName);
        }

        [Fact]
        public async Task TestDeleteTrainerAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe", 
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            _trainerRepository.DeleteTrainer(trainer);
            await _unitOfWork.Complete();

            Assert.False(_context.Trainer.Any(t => t.Id == trainer.Id));
        }

        [Fact]
        public async Task TestDoesEmailExistReturnsTrueForExistingEmailAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var exists = await _trainerRepository.DoesEmailExistAsync("john@example.com");

            Assert.True(exists);
        }

        [Fact]
        public async Task TestDoesEmailExistReturnsFalseForNonExistentEmailAsync()
        {
            var exists = await _trainerRepository.DoesEmailExistAsync("nonexistent@example.com");

            Assert.False(exists);
        }

        [Fact]
        public async Task TestDoesPhoneNumberExistReturnsTrueForExistingPhoneAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe",
                PhoneNumber = "1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var exists = await _trainerRepository.DoesPhoneNumberExistAsync("123 456 7890");

            Assert.True(exists);
        }

        [Fact]
        public async Task TestDoesPhoneNumberExistReturnsFalseForNonExistentPhoneAsync()
        {
            var exists = await _trainerRepository.DoesPhoneNumberExistAsync("9999999999");

            Assert.False(exists);
        }

        [Fact]
        public async Task TestDoesPhoneNumberExistHandlesSpacesAsync()
        {
            var trainer = new Trainer 
            { 
                FirstName = "john", 
                Surname = "doe",
                PhoneNumber = "1234567890",
                Role = UserRole.Trainer
            };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var exists = await _trainerRepository.DoesPhoneNumberExistAsync("123 456 7890");

            Assert.True(exists);
        }
    }
}
