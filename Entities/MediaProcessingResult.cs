namespace teachers_lounge_server.Entities
{
    public class MediaProcessingResult
    {
        public int StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public MediaItem? Data { get; set; }

        public static MediaProcessingResult Success(MediaItem data) => new()
        {
            StatusCode = 200,
            Data = data
        };

        public static MediaProcessingResult Failure(int statusCode, string errorMessage) => new()
        {
            StatusCode = statusCode,
            ErrorMessage = errorMessage
        };
    }

}
