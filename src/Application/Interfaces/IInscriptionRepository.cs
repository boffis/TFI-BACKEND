using GymManagement.Domain.Entities;

namespace GymManagement.Application.Interfaces
{
    public interface IInscriptionRepository
    {
        int CountByClassId(Guid classId);
        bool IsUserRepeated(Guid clientId, Guid classId);
        Inscription Add(Inscription inscription);
        void Remove(Guid clientId, Guid classId);
        List<Inscription> GetByClassId(Guid classId);
    }
}