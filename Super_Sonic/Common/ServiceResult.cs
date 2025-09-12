namespace Super_Sonic.Common
{
    public class ServiceResult<T>
    {
        public bool Succeeded { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public List<string> Errors { get; set; } = new List<string>();
        public T? Data { get; set; }

        public static ServiceResult<T> Success(T data, string message = "Operation successful")
            => new ServiceResult<T> { Succeeded = true, Message = message, Data = data };

        public static ServiceResult<T> Success(string message = "Operation successful")
            => new ServiceResult<T> { Succeeded = true, Message = message };

        public static ServiceResult<T> Failure(string message)
            => new ServiceResult<T> { Succeeded = false, Message = message };
        
        public static ServiceResult<T> Failure(List<string> errors)
            => new ServiceResult<T> { Succeeded = false, Message = "حدث خطأ ما" , Errors = errors };


    }


}
