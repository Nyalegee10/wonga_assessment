namespace UserAuth.Domain.Common;

public record TokenResult(string Token, DateTime ExpiresAt);
