using System;
using System.Collections.Generic;
using GymManagement.Application.Common;

namespace GymManagement.Application.Responses
{
    public class GymClassDetailResponse
    {
        /// <inheritdoc cref="GymClassResponse.HasStarted"/>
        public bool HasStarted => Schedule <= GymTime.Now;

        public Guid GymClassId { get; set; }
        public required string ClassName { get; set; }
        public string? ClassDescription { get; set; }
        public required int MaxCapacity { get; set; }
        public required DateTime Schedule { get; set; }
        public Guid? GymClassScheduleId { get; set; }

        public TrainerSummaryResponse Trainer { get; set; } = null!;
        public ICollection<ClientSummaryResponse> InscribedClients { get; set; } = [];
        public int InscriptionCount { get; set; }
        public bool IsCurrentUserInscribed { get; set; }
    }
}
