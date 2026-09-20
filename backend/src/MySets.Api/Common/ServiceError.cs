namespace MySets.Api.Common;

public sealed record ServiceError(ServiceErrorCode Code, string Message)
{
    public static ServiceError BadRequest(string message) => new(ServiceErrorCode.BadRequest, message);
    public static ServiceError ValidationError(string message) => new(ServiceErrorCode.ValidationError, message);
    public static ServiceError Unauthorized(string message) => new(ServiceErrorCode.Unauthorized, message);
    public static ServiceError Forbidden(string message) => new(ServiceErrorCode.Forbidden, message);
    public static ServiceError NotFound(string message) => new(ServiceErrorCode.NotFound, message);
    public static ServiceError Conflict(string message) => new(ServiceErrorCode.Conflict, message);
}
