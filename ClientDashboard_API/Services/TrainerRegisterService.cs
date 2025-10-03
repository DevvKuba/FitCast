using ClientDashboard_API.Entities;
using ClientDashboard_API.Interfaces;


namespace ClientDashboard_API.Services
{
    public sealed class TrainerRegisterService(ITrainerRepository trainerRepository, IPasswordHasher passwordHasher) : ITrainerRegisterService
    {
        public sealed record Request(string Email, string FirstName, string Surname, string Password);

        public async Task<Trainer> Handle(Request request)
        {
            if (await trainerRepository.DoesExistAsync(request.Email))
            {
                throw new Exception("The email is already in use");
            }

            var trainer = new Trainer
            {
                Email = request.Email,
                FirstName = request.FirstName,
                Surname = request.Surname,
                PasswordHash = passwordHasher.Hash(request.Password)
            };

            await trainerRepository.AddNewTrainerAsync(trainer);

            // email verification ?
            // access token 

            return trainer;
        }
    }
}
