using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace CRMWebApp.Authorization
{
    public static class Operations
    {
        public static OperationAuthorizationRequirement Edit =
            new() { Name = nameof(Edit) };
        public static OperationAuthorizationRequirement Delete =
            new() { Name = nameof(Delete) };
    }
}
