using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Dto_s;
using ClientDashboard_API.DTOs;
using ClientDashboard_API.Entities;
using ClientDashboard_API.Entities.ML.NET_Training_Entities;
using ClientDashboard_API.Enums;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ClientDashboard_API_Tests.RepositoryTests
{
    public class TrainerDailyRevenueRepositoryTests
    {
        private readonly DataContext _context;
        private readonly TrainerDailyRevenueRepository _trainerDailyRevenueRepository;
        private readonly UnitOfWork _unitOfWork;

        public TrainerDailyRevenueRepositoryTests()
        {
            var testUnitOfWork = new TestUnitOfWork();
            _context = testUnitOfWork.context;
            _trainerDailyRevenueRepository = testUnitOfWork.trainerDailyRevenueRepository;
            _unitOfWork = testUnitOfWork.unitOfWork;
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
        public async Task TestGetRecordedMonthsBeforeAsyncListsMonthsOldestFirstWithPartialFlagsAsync()
        {
            var trainer = new Trainer { FirstName = "john", Surname = "doe", Role = UserRole.Trainer };
            await _context.Trainer.AddAsync(trainer);
            await _unitOfWork.Complete();

            await AddDailyRecordsAsync(trainer.Id, new DateOnly(2026, 10, 1), 31);   // full October
            await AddDailyRecordsAsync(trainer.Id, new DateOnly(2026, 9, 20), 11);   // last 11 days of September
            await AddDailyRecordsAsync(trainer.Id, new DateOnly(2026, 11, 1), 5);    // the current month, excluded

            var months = await _trainerDailyRevenueRepository.GetRecordedMonthsBeforeAsync(trainer.Id, new DateOnly(2026, 11, 1));

            Assert.Equal(2, months.Count);
            Assert.Equal((2026, 9, 11, true), (months[0].Year, months[0].Month, months[0].RecordedDays, months[0].IsPartial));
            Assert.Equal((2026, 10, 31, false), (months[1].Year, months[1].Month, months[1].RecordedDays, months[1].IsPartial));
        }

        private async Task AddDailyRecordsAsync(int trainerId, DateOnly firstDay, int days)
        {
            for (int i = 0; i < days; i++)
            {
                await _context.TrainerDailyRevenue.AddAsync(new TrainerDailyRevenue { TrainerId = trainerId, AsOfDate = firstDay.AddDays(i) });
            }
            await _unitOfWork.Complete();
        }
    }
}

