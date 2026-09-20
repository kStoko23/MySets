namespace MySets.Api.Common;

public class ServiceResult
{
    public bool IsSuccess => Error is null;
    public ServiceError? Error { get; }

    protected ServiceResult(ServiceError? error)
    {
        Error = error;
    }
    public static ServiceResult Success() => new(null);

    public static implicit operator ServiceResult(ServiceError error) => new(error);
}

public sealed class ServiceResult<T> : ServiceResult
{
    public T? Data { get; }
    private ServiceResult(T? data, ServiceError? error) : base(error)
    {
        Data = data;
    }
    public static ServiceResult<T> Success(T data) => new(data, null);
    public static implicit operator ServiceResult<T>(ServiceError error) => new(default, error);
    public static implicit operator ServiceResult<T>(T data) => Success(data);
}
