namespace SubTrack.Domain.Common.Exceptions;

public sealed class EntityNotFoundException : AppException
{
    public EntityNotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.")
    {
        Entity = entity;
        Key = key;
    }

    public string Entity { get; }
    public object Key { get; }
}
