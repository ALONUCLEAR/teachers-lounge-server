using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using teachers_lounge_server.Entities;

namespace teachers_lounge_server.Services
{
    public struct MediaType
    {
        public const string JPG = "image/jpeg";
        public const string PNG = "image/png";

        public static bool isValid(string maybeMediaType)
        {
            return typeof(MediaType).GetFields().Some(field => field.Name.Equals(maybeMediaType));
        }

        public static string[] GetAllMediaTypes()
        {
            return typeof(MediaType).GetFields().Map(field => field.Name);
        }
    }
    public static class MediaService
    {
        private const int MaxFileSizeBytes = 15 * 1024 * 1024; // 15MB

        private static readonly HashSet<string> AllowedImageTypes = new() { MediaType.JPG, MediaType.PNG };

        public async static Task<MediaProcessingResult> ProcessMediaAsync(IFormFile file)
        {
            if (file.ContentType.StartsWith("image/"))
            {
                return await ProcessImageAsync(file);
            }

            return MediaProcessingResult.Failure(StatusCodes.Status415UnsupportedMediaType, "Unsupported media type.");
        }
        public static async Task<MediaProcessingResult> ProcessImageAsync(IFormFile file)
        {
            if (!AllowedImageTypes.Contains(file.ContentType.ToLower()))
            {
                return MediaProcessingResult.Failure(StatusCodes.Status415UnsupportedMediaType, "Unsupported media type.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return MediaProcessingResult.Failure(StatusCodes.Status413PayloadTooLarge, "File too large. Max allowed is 15MB.");
            }

            try
            {
                using var inputStream = file.OpenReadStream();
                using var image = await Image.LoadAsync<Rgba32>(inputStream); // loads both PNG and JPEG

                var jpgImage = ConvertToJpg(image);
                var data = await CompressJpg(jpgImage, 75);
                return MediaProcessingResult.Success(new MediaItem(data, MediaType.JPG));
            }
            catch (UnknownImageFormatException)
            {
                return MediaProcessingResult.Failure(StatusCodes.Status400BadRequest, "Could not read image format.");
            }
            catch
            {
                return MediaProcessingResult.Failure(StatusCodes.Status500InternalServerError, "Image processing failed.");
            }
        }

        private static Image<Rgba32> ConvertToJpg(Image<Rgba32> original)
        {
            // JPEG doesn't support transparency, so we flatten over white
            var backgroundColor = Rgba32.ParseHex("#FFFFFF");

            return original.Clone(ctx => ctx.BackgroundColor(backgroundColor));
        }

        private static async Task<byte[]> CompressJpg(Image<Rgba32> image, int quality)
        {
            using var outputStream = new MemoryStream();
            var encoder = new JpegEncoder { Quality = quality };

            await image.SaveAsJpegAsync(outputStream, encoder);
            return outputStream.ToArray();
        }
    }
}
