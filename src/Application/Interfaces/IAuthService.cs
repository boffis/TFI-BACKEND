using GymManagement.Application.Requests;
using GymManagement.Application.Responses;
using System.Threading.Tasks;

namespace GymManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<bool> SignUpAsync(UserRequest request, string baseUrl);

        AuthResponse? SignIn(SignInRequest request);

        bool ConfirmEmail(string email, string token);

        Task<bool> ForgotPasswordAsync(ForgotPasswordRequest request);

        bool ResetPassword(ResetPasswordRequest request);
    }
}