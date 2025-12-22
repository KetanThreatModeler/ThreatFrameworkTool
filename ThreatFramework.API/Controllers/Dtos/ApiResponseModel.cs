using System;

namespace ThreatModeler.TF.API.Controllers.Dtos
{
    public sealed class ApiResponseModel<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }

        public static ApiResponseModel<T> Success(T data, string? message = null) =>
      new() { IsSuccess = true, Data = data};

        public static ApiResponseModel<T> Fail(string message) =>
            new() { IsSuccess = false, Data = default};

    }
}
