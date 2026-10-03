using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class ClientDailyFeatureRepositoryTests
    {
        private readonly DataContext _context;
        private readonly ClientDailyFeatureRepository _clientDailyFeatureRepository;
        private readonly UnitOfWork _unitOfWork;

        public ClientDailyFeatureRepositoryTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _clientDailyFeatureRepository = testUnitOfWork.clientDailyFeatureRepository;
            _unitOfWork = testUnitOfWork.unitOfWork;
        }

        [Fact]
        public async Task TestAddNewRecordAsync()
        {
            var client = new Client
            {
                FirstName = "rob",
                Role = UserRole.Client,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var clientDailyData = new ClientDailyDataAddDto
            {
                AsOfDate = DateOnly.Parse("15/06/2024"),
                SessionsIn7d = 3,
                SessionsIn28d = 10,
                DaysSinceLastSession = 2,
                RemainingSessions = 4,
                AverageSessionDuration = 45.5,
                LifeTimeValue = 500.00m,
                CurrentlyActive = true,
                ClientId = client.Id
            };

            await _clientDailyFeatureRepository.AddNewRecordAsync(clientDailyData);
            await _unitOfWork.Complete();

            var savedRecord = await _context.ClientDailyFeature.FirstOrDefaultAsync();

            Assert.NotNull(savedRecord);
            Assert.Equal(DateOnly.Parse("15/06/2024"), savedRecord.AsOfDate);
            Assert.Equal(3, savedRecord.SessionsIn7d);
            Assert.Equal(10, savedRecord.SessionsIn28d);
            Assert.Equal(2, savedRecord.DaysSinceLastSession);
            Assert.Equal(4, savedRecord.RemainingSessions);
            Assert.Equal(45.5, savedRecord.AverageSessionDuration);
            Assert.Equal(500.00m, savedRecord.LifeTimeValue);
            Assert.True(savedRecord.CurrentlyActive);
            Assert.Equal(client.Id, savedRecord.ClientId);
        }

        [Fact]
        public async Task TestAddNewRecordWithNullDaysSinceLastSessionAsync()
        {
            var client = new Client
            {
                FirstName = "rob",
                Role = UserRole.Client,
                CurrentBlockSession = 1,
                TotalBlockSessions = 4,
                Workouts = []
            };
            await _context.Client.AddAsync(client);
            await _unitOfWork.Complete();

            var clientDailyData = new ClientDailyDataAddDto
            {
                AsOfDate = DateOnly.Parse("15/06/2024"),
                SessionsIn7d = 0,
                SessionsIn28d = 0,
                DaysSinceLastSession = null,
                RemainingSessions = 8,
                AverageSessionDuration = 0,
                LifeTimeValue = 0m,
                CurrentlyActive = true,
                ClientId = client.Id
            };

            await _clientDailyFeatureRepository.AddNewRecordAsync(clientDailyData);
            await _unitOfWork.Complete();

            var savedRecord = await _context.ClientDailyFeature.FirstOrDefaultAsync();

            Assert.NotNull(savedRecord);
            Assert.Null(savedRecord.DaysSinceLastSession);
            Assert.Equal(8, savedRecord.RemainingSessions);
        }
    }
}

