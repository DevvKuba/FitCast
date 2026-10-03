using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class UserRepositoryTests
    {
        private readonly DataContext _context;
        private readonly UserRepository _userRepository;
        private readonly UnitOfWork _unitOfWork;

        public UserRepositoryTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _userRepository = testUnitOfWork.userRepository;
            _unitOfWork = testUnitOfWork.unitOfWork;
        }

        [Fact]
        public async Task TestGetUserByEmailForTrainerAsync()
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

            var user = await _userRepository.GetUserByEmailAsync("john@example.com");

            Assert.NotNull(user);
            Assert.Equal("john@example.com", user.Email);
            Assert.Equal("john", user.FirstName);
            Assert.Equal(UserRole.Trainer, user.Role);
            Assert.IsType<Trainer>(user);
        }

        [Fact]
        public async Task TestGetUserByEmailForClientAsync()
        {
            var client = new Client
            {
                FirstName = "rob",
                Surname = "smith",
                Email = "rob@example.com",
                Role = UserRole.Client,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var user = await _userRepository.GetUserByEmailAsync("rob@example.com");

            Assert.NotNull(user);
            Assert.Equal("rob@example.com", user.Email);
            Assert.Equal("rob", user.FirstName);
            Assert.Equal(UserRole.Client, user.Role);
            Assert.IsType<Client>(user);
        }

        [Fact]
        public async Task TestGetUserByEmailReturnsNullForNonExistentEmailAsync()
        {
            var user = await _userRepository.GetUserByEmailAsync("nonexistent@example.com");

            Assert.Null(user);
        }

        [Fact]
        public async Task TestGetUserByEmailReturnsCorrectUserWhenMultipleUsersExistAsync()
        {
            var trainer = new Trainer
            {
                FirstName = "john",
                Surname = "doe",
                Email = "john@example.com",
                Role = UserRole.Trainer
            };
            var client = new Client
            {
                FirstName = "rob",
                Surname = "smith",
                Email = "rob@example.com",
                Role = UserRole.Client,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Trainer.AddAsync(trainer);
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var trainerUser = await _userRepository.GetUserByEmailAsync("john@example.com");
            var clientUser = await _userRepository.GetUserByEmailAsync("rob@example.com");

            Assert.NotNull(trainerUser);
            Assert.NotNull(clientUser);
            Assert.Equal(UserRole.Trainer, trainerUser.Role);
            Assert.Equal(UserRole.Client, clientUser.Role);
            Assert.IsType<Trainer>(trainerUser);
            Assert.IsType<Client>(clientUser);
        }
    }
}

