using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Application.Interfaces;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GymManagement.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MembershipPlanController : ControllerBase
    {
        private readonly IMembershipPlanService _membershipPlanService;

        public MembershipPlanController(IMembershipPlanService membershipPlanService)
        {
            _membershipPlanService = membershipPlanService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MembershipPlanResponse>>> GetAll()
        {
            var plans = await _membershipPlanService.GetAllPlansAsync();
            return Ok(plans);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<MembershipPlanResponse>> GetById(Guid id)
        {
            var plan = await _membershipPlanService.GetPlanByIdAsync(id);
            return Ok(plan);
        }

        /// <summary>Admin listing: unlike GetAll above, includes discontinued plans, flagged IsDeleted.</summary>
        [HttpGet("admin")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<ActionResult<IEnumerable<MembershipPlanResponse>>> GetAllForAdmin()
        {
            var plans = await _membershipPlanService.GetAllPlansForAdminAsync();
            return Ok(plans);
        }

        [HttpGet("admin/{id}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<ActionResult<AdminMembershipPlanResponse>> GetAdminById(Guid id)
        {
            var plan = await _membershipPlanService.GetAdminPlanByIdAsync(id);
            return Ok(plan);
        }

        [HttpPost]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<ActionResult<MembershipPlanResponse>> Create([FromBody] MembershipPlanRequest request)
        {
            var plan = await _membershipPlanService.CreatePlanAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = plan.MembershipPlanId }, plan);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<ActionResult<MembershipPlanResponse>> Update(Guid id, [FromBody] MembershipPlanRequest request)
        {
            var plan = await _membershipPlanService.UpdatePlanAsync(id, request);
            return Ok(plan);
        }

        /// <summary>Soft delete: subscribers keep their membership until it expires, only the charge stops.</summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<ActionResult> Delete(Guid id)
        {
            await _membershipPlanService.DeletePlanAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/restore")]
        [Authorize(Policy = Policies.OnlyAdmin)]
        public async Task<ActionResult> Restore(Guid id)
        {
            await _membershipPlanService.RestorePlanAsync(id);
            return NoContent();
        }
    }
}
