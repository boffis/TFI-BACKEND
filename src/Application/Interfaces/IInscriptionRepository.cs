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
        List<Inscription> GetByClientId(Guid clientId);
        void NullifyClientId(Guid inscriptionId);
        void RemoveById(Guid inscriptionId);

        /// <summary>
        /// Persists several inscriptions in one round trip. Used when a trainer saves a whole
        /// attendance roster — marking them one at a time would be one SaveChanges per client.
        /// </summary>
        void UpdateRange(IEnumerable<Inscription> inscriptions);
    }
}
