using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;
using GymManagement.Application.Exceptions;

namespace GymManagement.Application.Services
{
    public class MembershipPlanService : IMembershipPlanService
    {
        private readonly IMembershipPlanRepository _membershipPlanRepository;
        private readonly IMembershipRepository _membershipRepository;

        public MembershipPlanService(
            IMembershipPlanRepository membershipPlanRepository,
            IMembershipRepository membershipRepository)
        {
            _membershipPlanRepository = membershipPlanRepository;
            _membershipRepository = membershipRepository;
        }

        public async Task<IEnumerable<MembershipPlanResponse>> GetAllPlansAsync()
        {
            var plans = await _membershipPlanRepository.GetAllAsync();
            return plans.Select(p => new MembershipPlanResponse
            {
                MembershipPlanId = p.MembershipPlanId,
                Type = p.Type,
                Price = p.Price,
                DurationInDays = p.DurationInDays
            });
        }

        public async Task<MembershipPlanResponse> GetPlanByIdAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            return new MembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays
            };
        }

        public async Task<AdminMembershipPlanResponse> GetAdminPlanByIdAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            var memberships = await _membershipRepository.GetByPlanId(id);

            return new AdminMembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays,
                Memberships = memberships.Select(m => new MembershipSummaryResponse
                {
                    MembershipId = m.MembershipId,
                    ClientId = m.UserId,
                    ClientName = m.User.Name,
                    ClientEmail = m.User.Email,
                    IsCancelled = m.IsCancelled,
                    ExpirationDate = m.ExpirationDate
                }).ToList()
            };
        }

        public async Task<MembershipPlanResponse> CreatePlanAsync(MembershipPlanRequest request)
        {
            var plan = new MembershipPlan
            {
                MembershipPlanId = Guid.NewGuid(),
                Type = request.Type,
                Price = request.Price,
                DurationInDays = request.DurationInDays
            };

            await _membershipPlanRepository.AddAsync(plan);

            return new MembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays
            };
        }

        public async Task<MembershipPlanResponse> UpdatePlanAsync(Guid id, MembershipPlanRequest request)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            plan.Type = request.Type;
            plan.Price = request.Price;
            plan.DurationInDays = request.DurationInDays;

            await _membershipPlanRepository.UpdateAsync(plan);

            return new MembershipPlanResponse
            {
                MembershipPlanId = plan.MembershipPlanId,
                Type = plan.Type,
                Price = plan.Price,
                DurationInDays = plan.DurationInDays
            };
        }

        public async Task DeletePlanAsync(Guid id)
        {
            var plan = await _membershipPlanRepository.GetByIdAsync(id);
            if (plan == null)
                throw new NotFoundException($"Membership plan {id} not found.");

            await _membershipPlanRepository.DeleteAsync(id);
        }
    }
}
