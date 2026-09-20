using MySets.Api.Common;

namespace MySets.Api.Tests.Common;

public class ServiceResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = ServiceResult.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ImplicitConversion_FromServiceError_IsFailure()
    {
        ServiceResult result = ServiceError.NotFound("set not found");

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorCode.NotFound, result.Error!.Code);
        Assert.Equal("set not found", result.Error.Message);
    }

    [Fact]
    public void GenericSuccess_CarriesData()
    {
        var result = ServiceResult<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Data);
    }

    [Fact]
    public void GenericImplicitConversion_FromValue_IsSuccess()
    {
        ServiceResult<string> result = "7922-1";

        Assert.True(result.IsSuccess);
        Assert.Equal("7922-1", result.Data);
    }

    [Fact]
    public void GenericImplicitConversion_FromServiceError_IsFailureWithDefaultData()
    {
        ServiceResult<string> result = ServiceError.Conflict("already exists");

        Assert.False(result.IsSuccess);
        Assert.Equal(ServiceErrorCode.Conflict, result.Error!.Code);
        Assert.Null(result.Data);
    }
}
