using GymManagement.Application.Requests;
using GymManagement.Application.Responses;

namespace GymManagement.Application.Interfaces
{
    public interface IAuthService
    {
        AuthResponse? SignUpClient(UserRequest request);

        AuthResponse? SignUpTrainer(TrainerRequest request);

        AuthResponse? SignUpAdmin(UserRequest request);

        AuthResponse? SignIn(SignInRequest request);
    }
}