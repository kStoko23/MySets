using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MySets.Api.Common;

public static class ServiceResultHelper
{
    public static IActionResult ToActionResult(this ControllerBase controller, ServiceResult result) =>
        result.IsSuccess ? controller.NoContent() : ToProblem(controller, result.Error!);

    public static IActionResult ToActionResult<T>(this ControllerBase controller, ServiceResult<T> result) =>
        result.IsSuccess ? controller.Ok(result.Data) : ToProblem(controller, result.Error!);

    private static IActionResult ToProblem(ControllerBase controller, ServiceError error)
    {
        if (error.Code == ServiceErrorCode.Forbidden)
            return controller.Forbid();

        var statusCode = error.Code switch
        {
            ServiceErrorCode.BadRequest => StatusCodes.Status400BadRequest,
            ServiceErrorCode.ValidationError => StatusCodes.Status422UnprocessableEntity,
            ServiceErrorCode.Unauthorized => StatusCodes.Status401Unauthorized,
            ServiceErrorCode.NotFound => StatusCodes.Status404NotFound,
            ServiceErrorCode.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        return controller.Problem(detail: error.Message, statusCode: statusCode);
    }
}
