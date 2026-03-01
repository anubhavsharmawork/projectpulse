namespace Application.Common.Exceptions
{
    public class TenantNotFoundException : Exception
    {
        public TenantNotFoundException(string message) : base(message) { }
        public TenantNotFoundException(Guid tenantId)
            : base($"Tenant with ID '{tenantId}' was not found or is inactive.") { }
    }

    public class TenantLimitExceededException : Exception
    {
        public string Resource { get; }
        public int CurrentCount { get; }
        public int MaxCount { get; }

        public TenantLimitExceededException(string resource, int currentCount, int maxCount)
            : base($"{resource} limit reached ({currentCount}/{maxCount}). Upgrade your plan to increase limits.")
        {
            Resource = resource;
            CurrentCount = currentCount;
            MaxCount = maxCount;
        }
    }

    public class TenantInactiveException : Exception
    {
        public TenantInactiveException()
            : base("This tenant is inactive. Contact support for assistance.") { }
        public TenantInactiveException(string message) : base(message) { }
    }

    public class InvalidTenantContextException : Exception
    {
        public InvalidTenantContextException()
            : base("Tenant context is not available. Ensure the request includes valid tenant identification.") { }
        public InvalidTenantContextException(string message) : base(message) { }
    }

    public class UnauthorizedTenantAccessException : Exception
    {
        public UnauthorizedTenantAccessException()
            : base("You do not have access to this tenant.") { }
        public UnauthorizedTenantAccessException(string message) : base(message) { }
    }
}
