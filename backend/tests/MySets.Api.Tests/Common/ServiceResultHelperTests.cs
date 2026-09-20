using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using MySets.Api.Common;

namespace MySets.Api.Tests.Common;

public class ServiceResultHelperTests
{
    private sealed class TestController : ControllerBase;

    private readonly TestController _controller = CreateController();

    [Fact]
    public void ToActionResult_Success_ReturnsNoContent()
    {
        var actionResult = _controller.ToActionResult(ServiceResult.Success());

        Assert.IsType<NoContentResult>(actionResult);
    }

    [Fact]
    public void ToActionResult_GenericSuccess_ReturnsOkWithData()
    {
        ServiceResult<string> result = "hello";

        var actionResult = _controller.ToActionResult(result);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Equal("hello", ok.Value);
    }

    [Theory]
    [InlineData(ServiceErrorCode.BadRequest, StatusCodes.Status400BadRequest)]
    [InlineData(ServiceErrorCode.ValidationError, StatusCodes.Status422UnprocessableEntity)]
    [InlineData(ServiceErrorCode.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ServiceErrorCode.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ServiceErrorCode.Conflict, StatusCodes.Status409Conflict)]
    public void ToActionResult_Failure_MapsToProblemWithExpectedStatusCode(ServiceErrorCode code, int expectedStatus)
    {
        ServiceResult result = new ServiceError(code, "boom");

        var actionResult = _controller.ToActionResult(result);

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actionResult);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var problem = Assert.IsAssignableFrom<ProblemDetails>(objectResult.Value);
        Assert.Equal("boom", problem.Detail);
    }

    [Fact]
    public void ToActionResult_Forbidden_ReturnsForbidResultWithoutABody()
    {
        ServiceResult result = ServiceError.Forbidden("nope");

        var actionResult = _controller.ToActionResult(result);

        Assert.IsType<ForbidResult>(actionResult);
    }

    private static TestController CreateController()
    {
        var services = new ServiceCollection();
        services.AddMvcCore();
        var serviceProvider = services.BuildServiceProvider();

        return new TestController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = serviceProvider },
            },
        };
    }
}
