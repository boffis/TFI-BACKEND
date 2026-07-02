using GymManagement.Application.Requests;
using GymManagement.Application.Services;
using GymManagement.Presentation.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymManagement.Presentation.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = Policies.OnlyClient)]
    [ApiController]
    public class MembershipController : ControllerBase
    {
        private readonly MembershipService _membershipService;
        public MembershipController(MembershipService membershipService)
        {
            _membershipService = membershipService;
        }

            [HttpGet]
            public async Task<IActionResult> GetAll()
            {
                var memberships = await _membershipService.GetAllMemberships();
                return Ok(memberships);
            }

            [HttpGet("{MembershipId}")]
            public async Task<IActionResult> GetById(Guid membershipId)
            {
                var membership = await _membershipService.GetMembershipById(membershipId);
                if (membership == null) return NotFound();
                return Ok(membership);
            }

            [HttpPost("SelectMembership")]
            public async Task<IActionResult> SelectMembership([FromBody] MembershipRequest request)
            {
                var response = await _membershipService.AddMembership(request);
                return CreatedAtAction(nameof(GetById), new { id = response.MembershipId }, response);
            }

            [HttpPut("ChangeMembership/{MembershipId}")]
            public async Task<IActionResult> Change(Guid membershipId, [FromBody] NewMembershipRequest request)
            {
                var result = await _membershipService.ChangeMembershipAsync(membershipId, request);
                if (!result) return NotFound();
                return NoContent();
            }

            [HttpPost("ActivateMembership/{MembershipId}")]
            public async Task<IActionResult> ActivateMembership(Guid membershipId)
            {
                var result = await _membershipService.ActivateMembershipAsync(membershipId);
                if (!result) return NotFound();
                return Ok();
            }

            [HttpPost("CancelMembership/{MembershipId}")]
            public async Task<IActionResult> CancelMembership(Guid membershipId)
            {
                var result = await _membershipService.CancelMembershipAsync(membershipId);
                if (!result) return NotFound();
                return Ok();
            }
        }
    }
