namespace GymManagement.Presentation.Authorization
{
    public enum AuthorizationPolicy
    {
        OnlyAdmin,
        OnlyTrainer,
        OnlyClient,
    }

    public static class Policies
    {
        public const string OnlyAdmin = nameof(AuthorizationPolicy.OnlyAdmin);
        public const string OnlyTrainer = nameof(AuthorizationPolicy.OnlyTrainer);
        public const string OnlyClient = nameof(AuthorizationPolicy.OnlyClient);
    }
}