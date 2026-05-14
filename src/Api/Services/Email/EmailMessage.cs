namespace SubTrack.Api.Services.Email;

public sealed record EmailMessage(string To, string Subject, string Body);
