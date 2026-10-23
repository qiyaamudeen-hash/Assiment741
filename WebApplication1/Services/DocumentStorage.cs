namespace WebApplication1.Services;

public interface IDocumentStorage
{
    string? Validate(IFormFile? file);
    Task<string> SaveAsync(IFormFile file);
    string? GetPath(string storedFileName);
}

public class DocumentStorage : IDocumentStorage
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
    private const long MaxBytes = 5 * 1024 * 1024;

    private readonly string _folder;

    public DocumentStorage(IWebHostEnvironment env)
    {
        
        _folder = Path.Combine(env.ContentRootPath, "App_Data", "guest-documents");
        Directory.CreateDirectory(_folder);
    }

    
    public string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return "Please choose a file to upload.";
        if (file.Length > MaxBytes) return "The file is larger than 5 MB.";

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) return "Please upload a PDF, JPG or PNG file.";

        var header = new byte[4];
        using (var stream = file.OpenReadStream())
        {
            var read = stream.Read(header, 0, header.Length);
            if (read < header.Length) return "That file doesn't look like a valid PDF, JPG or PNG.";
        }

        var valid = ext switch
        {
            ".pdf" => header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46,   
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,   
            _ => header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF                             
        };

        return valid ? null : "That file doesn't look like a valid PDF, JPG or PNG.";
    }

    public async Task<string> SaveAsync(IFormFile file)
    {
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var storedName = $"{Guid.NewGuid()}{ext}";

        using var stream = new FileStream(Path.Combine(_folder, storedName), FileMode.Create);
        await file.CopyToAsync(stream);

        return storedName;
    }

    public string? GetPath(string storedFileName)
    {
      
        var path = Path.Combine(_folder, Path.GetFileName(storedFileName));
        return File.Exists(path) ? path : null;
    }
}