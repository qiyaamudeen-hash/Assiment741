namespace WebApplication1.Services;

public interface IImageStorage
{
    string? Validate(IEnumerable<IFormFile> files);
    Task<List<string>> SavePropertyImagesAsync(IEnumerable<IFormFile> files);
    void DeletePropertyImage(string imageUrl);
}

public class ImageStorage : IImageStorage
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private readonly IWebHostEnvironment _env;

    public ImageStorage(IWebHostEnvironment env)
    {
        _env = env;
    }
    public void DeletePropertyImage(string imageUrl)
    {
        // GetFileName strips any folder parts, so a bad URL can't point outside the uploads folder
        var fileName = Path.GetFileName(imageUrl);
        var path = Path.Combine(_env.WebRootPath, "uploads", "properties", fileName);
        if (File.Exists(path)) File.Delete(path);
    }
    // Returns an error message, or null if every file is OK
    public string? Validate(IEnumerable<IFormFile> files)
    {
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!AllowedExtensions.Contains(ext) || !file.ContentType.StartsWith("image/"))
                return $"'{file.FileName}' is not a supported image (use JPG, PNG or WEBP).";

            if (file.Length > MaxBytes)
                return $"'{file.FileName}' is larger than 5 MB.";
        }
        return null;
    }

    public async Task<List<string>> SavePropertyImagesAsync(IEnumerable<IFormFile> files)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "properties");
        Directory.CreateDirectory(folder);

        var urls = new List<string>();
        foreach (var file in files)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid()}{ext}";   // never trust the user's file name

            using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
            await file.CopyToAsync(stream);

            urls.Add($"/uploads/properties/{fileName}");
        }
        return urls;
    }
}