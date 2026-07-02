using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Domain.Enums;

namespace GymManagement.Application.Services
{
    public class MembershipService
    {
        private readonly IMembershipRepository _membershipRepository;
        private readonly IClientRepository _clientRepository;

        public MembershipService(IMembershipRepository membershipRepository, IClientRepository clientRepository)
        {
            _membershipRepository = membershipRepository;
            _clientRepository = clientRepository;
        }

        private static int GetDurationDays(MembershipType type)
        {
            return type switch
            {
                MembershipType.Weekly => 7,
                MembershipType.Monthly => 30,
                MembershipType.Quarterly => 90,
                MembershipType.Annual => 365,
                _ => 0
            };
        }

        public async Task<List<Membership>> GetAllMemberships()
            => await _membershipRepository.GetAllMemberships();

        public async Task<Membership?> GetMembershipById(Guid membershipId)
            => await _membershipRepository.GetMembershipById(membershipId);

        public async Task<MembershipResponse> AddMembership(MembershipRequest request)
        {
            var client = _clientRepository.GetById(request.ClientId)
                ?? throw new ArgumentException("Cliente no encontrado");

            var membership = new Membership
            {
                MembershipId = Guid.NewGuid(),
                ClientId = request.ClientId,
                Client = client,
                MembershipType = request.MembershipType,
                MembershipState = false,
                ExpirationDate = DateTime.MinValue,
                IsCancelled = false
            };

            await _membershipRepository.AddMembership(membership);

            return new MembershipResponse
            {
                MembershipId = membership.MembershipId,
                ClientId = membership.ClientId,
                MembershipType = membership.MembershipType,
                MembershipState = membership.MembershipState,
                ExpirationDate = membership.ExpirationDate
            };
        }

        public async Task<bool> ChangeMembershipAsync(Guid membershipId, NewMembershipRequest request)
        {
            var existingMembership = await _membershipRepository.GetMembershipById(membershipId);
            if (existingMembership == null) return false;

            existingMembership.MembershipType = request.MembershipType;
            existingMembership.ExpirationDate = DateTime.UtcNow.AddDays(GetDurationDays(request.MembershipType));

            await _membershipRepository.ChangeMembership(existingMembership);
            return true;
        }

        public async Task<bool> ActivateMembershipAsync(Guid membershipId)
        {
            var membership = await _membershipRepository.GetMembershipById(membershipId);
            if (membership == null) 
                return false;

            var durationDays = GetDurationDays(membership.MembershipType);

            membership.MembershipState = true;
            membership.ExpirationDate = DateTime.UtcNow.AddDays(durationDays);

            await _membershipRepository.ChangeMembership(membership);
            return true;
        }

        public async Task<bool> CancelMembershipAsync(Guid membershipId)
        {
            var membership = await _membershipRepository.GetMembershipById(membershipId);
            if (membership == null) 
                return false;
            membership.IsCancelled = true;
            await _membershipRepository.ChangeMembership(membership);
            return true;
        }

        public static void CheckMembershipExpiration(Membership membership)
        {
            if (DateTime.UtcNow > membership.ExpirationDate)
            {
                membership.MembershipState = false;
            }
        }
    }
}