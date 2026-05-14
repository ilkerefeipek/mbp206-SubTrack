namespace SubTrack.Api.Contracts;

public sealed record AuthResponse(string Token, DateTime ExpiresAt, UserDto User);
