using System;
using System.Collections.Generic;
using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IGymClassService
    {
        List<GymClassdto> GetAllClasses();
        List<GymClass> GetDeletedClasses();
        GymClass? GetClassById(Guid id);
        GymClassDetailResponse GetAdminClassById(Guid classId);
        GymClass? GetDeletedClassById(Guid id);
        GymClassResponse CreateClass(Guid trainerId, ClassRequest request);
        void ModifyClass(Guid classId, ClassRequest request, Guid requestingUserId, string userRole);
        void DeleteClass(Guid classId);
        List<GymClassResponse> GetTrainerClasses(Guid trainerId, Guid requestingUserId, string userRole);
        void JoinClass(Guid clientId, Guid classId, Guid requestingUserId, string userRole);
        void LeaveClass(Guid clientId, Guid classId, Guid requestingUserId, string userRole);
        List<Client> GetClientsByClass(Guid classId, Guid requestingUserId, string userRole);
        ScheduledAndSpecialClassesResponse GetScheduledAndSpecialClasses();
    }
}
