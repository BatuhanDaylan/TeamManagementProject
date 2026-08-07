namespace TeamManagement.Application.Common.Results
{
    public class Result
    {
        public bool IsSuccess { get; }
        public string Error { get; }
        public int StatusCode { get; }

        protected Result(bool isSuccess, string error, int statusCode)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }

        public static Result Success(int statusCode = 200) => new Result(true, string.Empty, statusCode);
        public static Result Failure(string error, int statusCode = 400) => new Result(false, error, statusCode);
    }

    public class Result<T> : Result
    {
        public T? Data { get; }

        private Result(bool isSuccess, T? data, string error, int statusCode)
            : base(isSuccess, error, statusCode)
        {
            Data = data;
        }

        public static Result<T> Success(T data, int statusCode = 200) => new Result<T>(true, data, string.Empty, statusCode);
        public static new Result<T> Failure(string error, int statusCode = 400) => new Result<T>(false, default, error, statusCode);
    }
}
