using GymManagement.Application.Requests;
using GymManagement.Application.Responses;

namespace GymManagement.Application.Interfaces
{
    public interface IAuthService
    {
        AuthResponse? SignUp(SignUpRequest request);
        AuthResponse? SignIn(SignInRequest request);
    }
}