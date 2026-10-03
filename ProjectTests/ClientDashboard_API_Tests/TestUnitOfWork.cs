using AutoMapper;
using ClientDashboard_API.Data;
using ClientDashboard_API.Helpers;
using ClientDashboard_API.Interfaces.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientDashboard_API_Tests
{
    internal class TestUnitOfWork
    {
        public IMapper mapper;
        public IPasswordHasher passwordHasher;
        public DataContext context;
        public UserRepository userRepository;
        public ClientRepository clientRepository;
        public WorkoutRepository workoutRepository;
        public TrainerRepository trainerRepository;
        public NotificationRepository notificationRepository;
        public NotificationRecipientStatusRepository notificationRecipientStatusRepository;
        public PaymentRepository paymentRepository;
        public EmailVerificationTokenRepository emailVerificationTokenRepository;
        public PasswordResetTokenRepository passwordResetTokenRepository;
        public ClientDailyFeatureRepository clientDailyFeatureRepository;
        public TrainerDailyRevenueRepository trainerDailyRevenueRepository;
        public BookingSeriesRepository bookingSeriesRepository;
        public BookedSessionSlotRepository bookedSessionSlotRepository;
        public UnitOfWork unitOfWork;

        public TestUnitOfWork()
        {
            mapper = TestMapperFactory.Create();
            passwordHasher = new PasswordHasher();

            var optionsBuilder = new DbContextOptionsBuilder<DataContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString());

            context = new DataContext(optionsBuilder.Options);
            userRepository = new UserRepository(context, passwordHasher);
            clientRepository = new ClientRepository(context, passwordHasher, mapper);
            workoutRepository = new WorkoutRepository(context);
            trainerRepository = new TrainerRepository(context, mapper);
            notificationRepository = new NotificationRepository(context);
            notificationRecipientStatusRepository = new NotificationRecipientStatusRepository(context);
            paymentRepository = new PaymentRepository(context, mapper);
            emailVerificationTokenRepository = new EmailVerificationTokenRepository(context);
            passwordResetTokenRepository = new PasswordResetTokenRepository(context);
            clientDailyFeatureRepository = new ClientDailyFeatureRepository(context);
            trainerDailyRevenueRepository = new TrainerDailyRevenueRepository(context, mapper);
            bookingSeriesRepository = new BookingSeriesRepository(context);
            bookedSessionSlotRepository = new BookedSessionSlotRepository(context);
            unitOfWork = new UnitOfWork(context, userRepository, clientRepository, workoutRepository, trainerRepository, notificationRepository, notificationRecipientStatusRepository, paymentRepository, emailVerificationTokenRepository, clientDailyFeatureRepository, trainerDailyRevenueRepository, passwordResetTokenRepository, bookingSeriesRepository, bookedSessionSlotRepository);
        }
    }
}
