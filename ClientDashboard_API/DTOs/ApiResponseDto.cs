namespace ClientDashboard_API.DTOs
{
    public class ApiResponseDto<T>
    {
        public T? Data { get; set; }
        public required bool Success { get; set; }
        public required string Message { get; set; }
    }
}
