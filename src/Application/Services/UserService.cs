using GymManagement.Application.Interfaces;
using GymManagement.Application.Mappers;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Application.Exceptions;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IClientRepository _clientRepository;
        private readonly ITrainerRepository _trainerRepository;
        private readonly IAdminRepository _adminRepository;
        private readonly IMembershipRepository _membershipRepository;
        private readonly IInscriptionRepository _inscriptionRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IGymClassRepository _gymClassRepository;

        public UserService(
            IClientRepository clientRepository,
            ITrainerRepository trainerRepository,
            IAdminRepository adminRepository,
            IMembershipRepository membershipRepository,
            IInscriptionRepository inscriptionRepository,
            IPaymentRepository paymentRepository,
            IGymClassRepository gymClassRepository)
        {
            _clientRepository = clientRepository;
            _trainerRepository = trainerRepository;
            _adminRepository = adminRepository;
            _membershipRepository = membershipRepository;
            _inscriptionRepository = inscriptionRepository;
            _paymentRepository = paymentRepository;
            _gymClassRepository = gymClassRepository;
        }

        public GetAllUsersResponse GetAll()
        {
            ExpireUnconfirmedUsers();

            var clients = _clientRepository.GetAll()
                .Select(c =>
                {
                    var activeMembership = _membershipRepository.GetActiveByUserId(c.UserId).Result;
                    var isMembershipActive = activeMembership != null
                        && activeMembership.ExpirationDate > DateTime.UtcNow;
                    return c.ToClientResponse(isMembershipActive);
                })
                .ToList();

            var trainers = _trainerRepository.GetAll()
                .Select(t => t.ToTrainerResponse())
                .ToList();

            var admins = _adminRepository.GetAll()
                .Select(a => a.ToUserResponse())
                .ToList();

            return new GetAllUsersResponse
            {
                ClientList = clients,
                TrainerList = trainers,
                AdminList = admins
            };
        }

        public List<UserResponse> GetAllDeleted()
        {
            var users = new List<UserResponse>();
            users.AddRange(_clientRepository.GetDeleteds().Select(c => c.ToUserResponse()));
            users.AddRange(_trainerRepository.GetDeleteds().Select(t => TrainerMapper.ToTrainerResponse(t)));
            users.AddRange(_adminRepository.GetDeleteds().Select(a => a.ToUserResponse()));
            return users;
        }

        public UserResponse? GetById(Guid id)
        {
            var user = GetUserEntityById(id);
            if (user == null) return null;

            if (user is Trainer trainer) return TrainerMapper.ToTrainerResponse(trainer);
            return user.ToUserResponse();
        }

        public UserDetailedResponse? GetDetailedById(Guid id)
        {
            var user = GetUserEntityById(id) ?? GetDeletedUserEntityById(id);
            if (user == null) return null;

            UserResponse baseResponse;
            if (user is Trainer trainer) baseResponse = TrainerMapper.ToTrainerResponse(trainer);
            else baseResponse = user.ToUserResponse();

            var detailedResponse = new UserDetailedResponse
            {
                UserId = baseResponse.UserId,
                Name = baseResponse.Name,
                Email = baseResponse.Email,
                DateOfBirth = baseResponse.DateOfBirth,
                DNI = baseResponse.DNI,
                Gender = baseResponse.Gender,
                PhoneNumber = baseResponse.PhoneNumber,
                Role = baseResponse.Role,
                Payments = _paymentRepository.GetPaymentsByUserId(id).Select(p => new PaymentResponse
                {
                    PaymentId = p.PaymentId,
                    UserId = p.UserId,
                    MembershipId = p.MembershipId,
                    Price = p.Price,
                    PaymentDate = p.PaymentDate,
                    PaymentMethod = p.PaymentMethod
                }).ToList(),
                Memberships = _membershipRepository.GetByUserId(id).Result.Select(m => new MembershipResponse
                {
                    MembershipId = m.MembershipId,
                    UserId = m.UserId,
                    ExpirationDate = m.ExpirationDate,
                    MembershipPlan = new MembershipPlanResponse
                    {
                        MembershipPlanId = m.MembershipPlan.MembershipPlanId,
                        Type = m.MembershipPlan.Type,
                        Price = m.MembershipPlan.Price,
                        DurationInDays = m.MembershipPlan.DurationInDays
                    }
                }).ToList()
            };

            if (user is Client)
            {
                detailedResponse.Inscriptions = _inscriptionRepository.GetByClientId(id).Select(i => new InscriptionResponse
                {
                    InscriptionId = i.InscriptionId,
                    GymClassId = i.GymClassId
                }).ToList();
            }
            else if (user is Trainer trainerEntity)
            {
                detailedResponse.Specialization = trainerEntity.Specialization;
            }

            var taughtClasses = _gymClassRepository.GetByTrainerId(id);
            if (taughtClasses.Count > 0)
            {
                detailedResponse.TaughtClasses = taughtClasses.Select(c => new GymClassResponse
                {
                    GymClassId = c.GymClassId,
                    ClassName = c.ClassName,
                    ClassDescription = c.ClassDescription,
                    MaxCapacity = c.MaxCapacity,
                    TrainerId = c.TrainerId,
                    Schedule = c.Schedule
                }).ToList();
            }

            return detailedResponse;
        }

        public UserResponse? GetDeletedById(Guid id)
        {
            var user = GetDeletedUserEntityById(id);
            if (user == null) return null;

            if (user is Trainer trainer) return TrainerMapper.ToTrainerResponse(trainer);
            return user.ToUserResponse();
        }

        public bool Update(Guid id, UserRequest request)
        {
            var user = GetUserEntityById(id);
            if (user == null) return false;

            user.Name = request.Name;
            user.Email = request.Email;
            user.DateOfBirth = request.DateOfBirth;
            user.DNI = request.DNI;
            user.Gender = request.Gender;
            user.PhoneNumber = request.PhoneNumber;

            if (user is Client client) _clientRepository.Update(client);
            else if (user is Trainer trainer)
            {
                trainer.Specialization = request.Specialization;
                _trainerRepository.Update(trainer);
            }
            else if (user is Admin admin) _adminRepository.Update(admin);

            return true;
        }

        public bool Delete(Guid id)
        {
            var user = GetUserEntityById(id);
            if (user == null) return false;

            if (user is Client) _clientRepository.Delete(id);
            else if (user is Trainer) _trainerRepository.Delete(id);
            else if (user is Admin) _adminRepository.Delete(id);

            return true;
        }

        public bool Recover(Guid id)
        {
            var user = GetDeletedUserEntityById(id);
            if (user == null) return false;

            if (user is Client) _clientRepository.Recover(id);
            else if (user is Trainer) _trainerRepository.Recover(id);
            else if (user is Admin) _adminRepository.Recover(id);

            return true;
        }

        public bool ChangeRole(Guid id, string newRole, string? specialization = null)
        {
            var user = GetUserEntityById(id);
            if (user == null) return false;

            var currentRole = user.GetType().Name;
            if (currentRole.Equals(newRole, StringComparison.OrdinalIgnoreCase)) return true;

            // 1. If current role is Client, handle membership and inscriptions
            if (user is Client)
            {
                // Cancel active membership
                var activeMembership = _membershipRepository.GetActiveByUserId(id).Result;
                if (activeMembership != null)
                    _membershipRepository.CancelMembership(activeMembership.MembershipId).Wait();

                // Handle inscriptions: delete future ones (free the spot), nullify past ones (keep attendance record)
                var inscriptions = _inscriptionRepository.GetByClientId(id);
                foreach (var inscription in inscriptions)
                {
                    if (inscription.GymClass.Schedule > DateTime.UtcNow)
                        _inscriptionRepository.RemoveById(inscription.InscriptionId);
                    else
                        _inscriptionRepository.NullifyClientId(inscription.InscriptionId);
                }
            }

            // 2. Hard-delete the old role subtype row
            //    For Trainers: block if they still have future classes assigned
            if (user is Trainer)
            {
                var hasFutureClasses = _gymClassRepository.GetByTrainerId(id)
                    .Any(gc => gc.Schedule > DateTime.UtcNow);
                if (hasFutureClasses)
                    throw new ConflictException(
                        "El entrenador tiene clases futuras asignadas. Reasígnalas o elimínalas antes de cambiar el rol.");
            }

            if (user is Client) _clientRepository.HardDelete(id);
            else if (user is Trainer) _trainerRepository.HardDelete(id);
            else if (user is Admin) _adminRepository.HardDelete(id);

            // 3. Insert the new role row, copying all base User fields
            switch (newRole.ToLower())
            {
                case "client":
                    _clientRepository.Add(new Client
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        Password = user.Password,
                        DateOfBirth = user.DateOfBirth,
                        DNI = user.DNI,
                        Gender = user.Gender,
                        PhoneNumber = user.PhoneNumber,
                        IsUserDeleted = user.IsUserDeleted,
                        IsEmailConfirmed = user.IsEmailConfirmed,
                        EmailConfirmationToken = user.EmailConfirmationToken,
                        EmailConfirmationTokenExpiration = user.EmailConfirmationTokenExpiration,
                        PasswordResetToken = user.PasswordResetToken,
                        PasswordResetTokenExpiration = user.PasswordResetTokenExpiration,
                    });
                    break;

                case "trainer":
                    _trainerRepository.Add(new Trainer
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        Password = user.Password,
                        DateOfBirth = user.DateOfBirth,
                        DNI = user.DNI,
                        Gender = user.Gender,
                        PhoneNumber = user.PhoneNumber,
                        IsUserDeleted = user.IsUserDeleted,
                        IsEmailConfirmed = user.IsEmailConfirmed,
                        EmailConfirmationToken = user.EmailConfirmationToken,
                        EmailConfirmationTokenExpiration = user.EmailConfirmationTokenExpiration,
                        PasswordResetToken = user.PasswordResetToken,
                        PasswordResetTokenExpiration = user.PasswordResetTokenExpiration,
                        Specialization = specialization
                    });
                    break;

                case "admin":
                    _adminRepository.Add(new Admin
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Email = user.Email,
                        Password = user.Password,
                        DateOfBirth = user.DateOfBirth,
                        DNI = user.DNI,
                        Gender = user.Gender,
                        PhoneNumber = user.PhoneNumber,
                        IsUserDeleted = user.IsUserDeleted,
                        IsEmailConfirmed = user.IsEmailConfirmed,
                        EmailConfirmationToken = user.EmailConfirmationToken,
                        EmailConfirmationTokenExpiration = user.EmailConfirmationTokenExpiration,
                        PasswordResetToken = user.PasswordResetToken,
                        PasswordResetTokenExpiration = user.PasswordResetTokenExpiration,
                    });
                    break;

                default:
                    throw new ArgumentException($"Unknown role: {newRole}");
            }

            return true;
        }

        public List<ActiveTrainerResponse> GetActiveTrainers()
        {
            return _trainerRepository.GetAll()
                .Select(t => new ActiveTrainerResponse
                {
                    UserId = t.UserId,
                    Name = t.Name,
                    Specialization = t.Specialization
                })
                .ToList();
        }

        private User? GetUserEntityById(Guid id)
        {
            var client = _clientRepository.GetById(id);
            if (client != null) return client;

            var trainer = _trainerRepository.GetById(id);
            if (trainer != null) return trainer;

            var admin = _adminRepository.GetById(id);
            if (admin != null) return admin;

            return null;
        }

        private User? GetDeletedUserEntityById(Guid id)
        {
            var client = _clientRepository.GetDeletedById(id);
            if (client != null) return client;

            var trainer = _trainerRepository.GetDeletedById(id);
            if (trainer != null) return trainer;

            var admin = _adminRepository.GetDeletedById(id);
            if (admin != null) return admin;

            return null;
        }

        private void ExpireUnconfirmedUsers()
        {
            var now = DateTime.UtcNow;

            foreach (var client in _clientRepository.GetAll())
                if (!client.IsEmailConfirmed
                    && client.EmailConfirmationTokenExpiration.HasValue
                    && client.EmailConfirmationTokenExpiration.Value < now)
                {
                    client.IsUserDeleted = true;
                    _clientRepository.Update(client);
                }

            foreach (var trainer in _trainerRepository.GetAll())
                if (!trainer.IsEmailConfirmed
                    && trainer.EmailConfirmationTokenExpiration.HasValue
                    && trainer.EmailConfirmationTokenExpiration.Value < now)
                {
                    trainer.IsUserDeleted = true;
                    _trainerRepository.Update(trainer);
                }

            foreach (var admin in _adminRepository.GetAll())
                if (!admin.IsEmailConfirmed
                    && admin.EmailConfirmationTokenExpiration.HasValue
                    && admin.EmailConfirmationTokenExpiration.Value < now)
                {
                    admin.IsUserDeleted = true;
                    _adminRepository.Update(admin);
                }
        }
    }
}
