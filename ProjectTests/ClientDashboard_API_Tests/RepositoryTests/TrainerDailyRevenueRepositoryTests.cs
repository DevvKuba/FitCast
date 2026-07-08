using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class TrainerDailyRevenueRepositoryTests
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

        public TrainerDailyRevenueRepositoryTests()
        {
            _mapper = TestMapperFactory.Create();
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
            _trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(_context, _mapper);
            _unitOfWork = new UnitOfWork(_context, _userRepository, _clientRepository, _workoutRepository, _trainerRepository, _notificationRepository, new NotificationRecipientStatusRepository(_context), _paymentRepository, _emailVerificationTokenRepository, _clientDailyFeatureRepository, _trainerDailyRevenueRepository, _passwordResetTokenRepository);
        }

        [Fact]
        public async Task TestAddTrainerDailyRevenueRecordAsync()
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

            var trainerDailyData = new TrainerDailyRevenue
            {
                TrainerId = trainer.Id,
                RevenueToday = 250.00m,
                MonthlyRevenueThusFar = 3500.00m,
                TotalSessionsThisMonth = 45,
                NewClientsThisMonth = 3,
                ActiveClients = 12,
                AverageSessionPrice = 75.00m,
                AsOfDate = DateOnly.Parse("15/06/2025")
            };

            await _trainerDailyRevenueRepository.AddTrainerDailyRevenueRecordAsync(trainerDailyData);
            await _unitOfWork.Complete();

            var savedRecord = await _context.TrainerDailyRevenue.FirstOrDefaultAsync();

            Assert.NotNull(savedRecord);
            Assert.Equal(trainer.Id, savedRecord.TrainerId);
            Assert.Equal(250.00m, savedRecord.RevenueToday);
            Assert.Equal(3500.00m, savedRecord.MonthlyRevenueThusFar);
            Assert.Equal(45, savedRecord.TotalSessionsThisMonth);
            Assert.Equal(3, savedRecord.NewClientsThisMonth);
            Assert.Equal(12, savedRecord.ActiveClients);
            Assert.Equal(75.00m, savedRecord.AverageSessionPrice);
            Assert.Equal(DateOnly.Parse("15/06/2025"), savedRecord.AsOfDate);
        }

        [Fact]
        public async Task TestAddTrainerDailyRevenueRecordWithZeroRevenueAsync()
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

            var trainerDailyData = new TrainerDailyRevenue
            {
                TrainerId = trainer.Id,
                RevenueToday = 0m,
                MonthlyRevenueThusFar = 1000.00m,
                TotalSessionsThisMonth = 10,
                NewClientsThisMonth = 0,
                ActiveClients = 5,
                AverageSessionPrice = 50.00m,
                AsOfDate = DateOnly.Parse("01/06/2025")
            };

            await _trainerDailyRevenueRepository.AddTrainerDailyRevenueRecordAsync(trainerDailyData);
            await _unitOfWork.Complete();

            var savedRecord = await _context.TrainerDailyRevenue.FirstOrDefaultAsync();

            Assert.NotNull(savedRecord);
            Assert.Equal(0m, savedRecord.RevenueToday);
            Assert.Equal(0, savedRecord.NewClientsThisMonth);
            Assert.Equal(1000.00m, savedRecord.MonthlyRevenueThusFar);
        }

        [Fact]
        public async Task TestAddMultipleTrainerDailyRevenueRecordsAsync()
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

            var record1 = new TrainerDailyRevenue
            {
                TrainerId = trainer.Id,
                RevenueToday = 100.00m,
                MonthlyRevenueThusFar = 500.00m,
                TotalSessionsThisMonth = 10,
                NewClientsThisMonth = 1,
                ActiveClients = 5,
                AverageSessionPrice = 50.00m,
                AsOfDate = DateOnly.Parse("01/06/2025")
            };

            var record2 = new TrainerDailyRevenue
            {
                TrainerId = trainer.Id,
                RevenueToday = 150.00m,
                MonthlyRevenueThusFar = 650.00m,
                TotalSessionsThisMonth = 13,
                NewClientsThisMonth = 1,
                ActiveClients = 5,
                AverageSessionPrice = 50.00m,
                AsOfDate = DateOnly.Parse("02/06/2025")
            };

            await _trainerDailyRevenueRepository.AddTrainerDailyRevenueRecordAsync(record1);
            await _trainerDailyRevenueRepository.AddTrainerDailyRevenueRecordAsync(record2);
            await _unitOfWork.Complete();

            var savedRecords = await _context.TrainerDailyRevenue.ToListAsync();

            Assert.Equal(2, savedRecords.Count);
            Assert.Contains(savedRecords, r => r.AsOfDate == DateOnly.Parse("01/06/2025"));
            Assert.Contains(savedRecords, r => r.AsOfDate == DateOnly.Parse("02/06/2025"));
            Assert.All(savedRecords, r => Assert.Equal(trainer.Id, r.TrainerId));
        }

        [Fact]
        public async Task TestAddTrainerDailyRevenueRecordWithHighRevenueAsync()
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

            var trainerDailyData = new TrainerDailyRevenue
            {
                TrainerId = trainer.Id,
                RevenueToday = 1500.00m,
                MonthlyRevenueThusFar = 25000.00m,
                TotalSessionsThisMonth = 200,
                NewClientsThisMonth = 15,
                ActiveClients = 50,
                AverageSessionPrice = 125.00m,
                AsOfDate = DateOnly.Parse("30/06/2025")
            };

            await _trainerDailyRevenueRepository.AddTrainerDailyRevenueRecordAsync(trainerDailyData);
            await _unitOfWork.Complete();

            var savedRecord = await _context.TrainerDailyRevenue.FirstOrDefaultAsync();

            Assert.NotNull(savedRecord);
            Assert.Equal(1500.00m, savedRecord.RevenueToday);
            Assert.Equal(25000.00m, savedRecord.MonthlyRevenueThusFar);
            Assert.Equal(200, savedRecord.TotalSessionsThisMonth);
            Assert.Equal(15, savedRecord.NewClientsThisMonth);
            Assert.Equal(50, savedRecord.ActiveClients);
            Assert.Equal(125.00m, savedRecord.AverageSessionPrice);
        }

        [Fact]
        public async Task TestDoesTrainerDailyRevenueRecordExistForDateAsyncReturnsFalseWhenNoRecordExists()
        {
            var trainer = new Trainer { FirstName = "test", Surname = "trainer", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var exists = await _trainerDailyRevenueRepository.DoesTrainerDailyRevenueRecordExistForDateAsync(trainer.Id, new DateOnly(2024, 6, 15));

            Assert.False(exists);
        }

        [Fact]
        public async Task TestDoesTrainerDailyRevenueRecordExistForDateAsyncReturnsTrueWhenRecordExists()
        {
            var trainer = new Trainer { FirstName = "test", Surname = "trainer", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var date = new DateOnly(2024, 6, 15);
            var dto = new TrainerDailyRevenueDto
            {
                TrainerId = trainer.Id,
                AsOfDate = date,
                RevenueToday = 50.00m,
                MonthlyRevenueThusFar = 50.00m,
                SessionsToday = 1,
                TotalSessionsThisMonth = 1,
                NewClientsThisMonth = 0,
                TotalSessionDuration = 45,
                ActiveClients = 1,
                AverageSessionPrice = 50.00m
            };
            await _trainerDailyRevenueRepository.AddTrainerDailyRevenueDtoRecordAsync(dto);
            await _unitOfWork.Complete();

            var exists = await _trainerDailyRevenueRepository.DoesTrainerDailyRevenueRecordExistForDateAsync(trainer.Id, date);

            Assert.True(exists);
        }

        [Fact]
        public async Task TestUpdateTrainerRevenueRecordAtDateAsyncOverwritesExistingRecordAsync()
        {
            var trainer = new Trainer { FirstName = "test", Surname = "trainer", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            var date = new DateOnly(2024, 6, 15);
            var initialDto = new TrainerDailyRevenueDto
            {
                TrainerId = trainer.Id,
                AsOfDate = date,
                RevenueToday = 50.00m,
                MonthlyRevenueThusFar = 50.00m,
                SessionsToday = 1,
                TotalSessionsThisMonth = 1,
                NewClientsThisMonth = 0,
                TotalSessionDuration = 45,
                ActiveClients = 1,
                AverageSessionPrice = 50.00m
            };
            await _trainerDailyRevenueRepository.AddTrainerDailyRevenueDtoRecordAsync(initialDto);
            await _unitOfWork.Complete();

            var updatedDto = new TrainerDailyRevenueDto
            {
                TrainerId = trainer.Id,
                AsOfDate = date,
                RevenueToday = 150.00m,
                MonthlyRevenueThusFar = 150.00m,
                SessionsToday = 3,
                TotalSessionsThisMonth = 3,
                NewClientsThisMonth = 1,
                TotalSessionDuration = 130,
                ActiveClients = 2,
                AverageSessionPrice = 50.00m
            };

            await _trainerDailyRevenueRepository.UpdateTrainerRevenueRecordAtDateAsync(updatedDto);
            await _unitOfWork.Complete();

            var records = await _context.TrainerDailyRevenue.ToListAsync();
            Assert.Single(records); // overwritten, not duplicated

            var record = records[0];
            Assert.Equal(150.00m, record.RevenueToday);
            Assert.Equal(3, record.SessionsToday);
            Assert.Equal(2, record.ActiveClients);
        }

        [Fact]
        public void TestIsFullMonthPresentReturnsFalseForEmptyList()
        {
            var result = _trainerDailyRevenueRepository.IsFullMonthPresent(new List<TrainerDailyRevenue>());

            Assert.False(result);
        }

        [Fact]
        public void TestIsFullMonthPresentReturnsFalseForPartialMonth()
        {
            // Only the first 5 days of a 30-day month
            var records = new List<TrainerDailyRevenue>();
            for (int day = 1; day <= 5; day++)
            {
                records.Add(new TrainerDailyRevenue { TrainerId = 1, AsOfDate = new DateOnly(2024, 6, day), AverageSessionPrice = 50.00m });
            }

            var result = _trainerDailyRevenueRepository.IsFullMonthPresent(records);

            Assert.False(result);
        }

        [Fact]
        public void TestIsFullMonthPresentReturnsTrueForCompleteMonth()
        {
            var daysInJune = DateTime.DaysInMonth(2024, 6);
            var records = new List<TrainerDailyRevenue>();
            for (int day = 1; day <= daysInJune; day++)
            {
                records.Add(new TrainerDailyRevenue { TrainerId = 1, AsOfDate = new DateOnly(2024, 6, day), AverageSessionPrice = 50.00m });
            }

            var result = _trainerDailyRevenueRepository.IsFullMonthPresent(records);

            Assert.True(result);
        }

        [Fact]
        public void TestIsFullMonthPresentReturnsFalseForCrossYearRecordsSharingMonthNumber()
        {
            // 15 days from March 2024 + 16 days from March 2025 = 31 records, matching March's
            // day count, but spanning two different calendar years - not one complete month.
            var records = new List<TrainerDailyRevenue>();
            for (int day = 1; day <= 15; day++)
            {
                records.Add(new TrainerDailyRevenue { TrainerId = 1, AsOfDate = new DateOnly(2024, 3, day), AverageSessionPrice = 50.00m });
            }
            for (int day = 16; day <= 31; day++)
            {
                records.Add(new TrainerDailyRevenue { TrainerId = 1, AsOfDate = new DateOnly(2025, 3, day), AverageSessionPrice = 50.00m });
            }

            var result = _trainerDailyRevenueRepository.IsFullMonthPresent(records);

            Assert.False(result);
        }
    }
}

