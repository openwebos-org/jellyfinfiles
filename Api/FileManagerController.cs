using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.WebFileManager.Api;

[ApiController]
[Route("WebFileManager")]
[Authorize]
public sealed class FileManagerController : ControllerBase
{
    private static readonly HashSet<string> DangerousNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ".", ".."
    };

    private string RootPath =>
        Path.GetFullPath(
            string.IsNullOrWhiteSpace(Plugin.Instance?.Configuration.RootPath)
                ? "/media"
                : Plugin.Instance.Configuration.RootPath);

    private string ResolvePath(string? relativePath)
    {
        relativePath ??= string.Empty;
        relativePath = relativePath.Replace('\\', '/').TrimStart('/');

        if (relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Any(DangerousNames.Contains))
        {
            throw new UnauthorizedAccessException("Invalid path.");
        }

        var root = RootPath;
        var combined = Path.GetFullPath(Path.Combine(root, relativePath));

        if (!string.Equals(combined, root, StringComparison.Ordinal)
            && !combined.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Path escapes the configured root.");
        }

        return combined;
    }

    [HttpGet("list")]
    public ActionResult<IEnumerable<FileEntry>> List([FromQuery] string? path = "")
    {
        var directory = ResolvePath(path);

        if (!Directory.Exists(directory))
        {
            return NotFound();
        }

        var entries = Directory.EnumerateFileSystemEntries(directory)
            .Select(fullPath =>
            {
                var isDirectory = Directory.Exists(fullPath);
                var info = isDirectory
                    ? (FileSystemInfo)new DirectoryInfo(fullPath)
                    : new FileInfo(fullPath);

                return new FileEntry(
                    Path.GetRelativePath(RootPath, fullPath).Replace(Path.DirectorySeparatorChar, '/'),
                    Path.GetFileName(fullPath),
                    isDirectory,
                    isDirectory ? null : ((FileInfo)info).Length,
                    info.LastWriteTimeUtc);
            })
            .OrderByDescending(x => x.IsDirectory)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase);

        return Ok(entries);
    }

    [HttpPost("folder")]
    public IActionResult CreateFolder([FromBody] PathRequest request)
    {
        var path = ResolvePath(request.Path);
        Directory.CreateDirectory(path);
        return NoContent();
    }

    [HttpPost("upload")]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<IActionResult> Upload(
        [FromQuery] string path,
        [FromQuery] string fileName,
        CancellationToken cancellationToken)
    {
        ValidateLeafName(fileName);

        var directory = ResolvePath(path);
        Directory.CreateDirectory(directory);

        var destination = ResolvePath(
            Path.Combine(
                Path.GetRelativePath(RootPath, directory),
                fileName));

        await using var output = new FileStream(
            destination,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            1024 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await Request.Body.CopyToAsync(output, cancellationToken);
        return Ok();
    }

    [HttpGet("download")]
    public IActionResult Download([FromQuery] string path)
    {
        var file = ResolvePath(path);

        if (!System.IO.File.Exists(file))
        {
            return NotFound();
        }

        var name = Path.GetFileName(file);
        return PhysicalFile(file, "application/octet-stream", name, enableRangeProcessing: true);
    }

    [HttpPost("rename")]
    public IActionResult Rename([FromBody] RenameRequest request)
    {
        ValidateLeafName(request.NewName);

        var source = ResolvePath(request.Path);
        var parent = Path.GetDirectoryName(source)!;
        var target = ResolvePath(
            Path.Combine(
                Path.GetRelativePath(RootPath, parent),
                request.NewName));

        if (System.IO.File.Exists(source))
        {
            System.IO.File.Move(source, target);
        }
        else if (Directory.Exists(source))
        {
            Directory.Move(source, target);
        }
        else
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpPost("move")]
    public IActionResult Move([FromBody] MoveRequest request)
    {
        var source = ResolvePath(request.Path);
        var destinationDirectory = ResolvePath(request.Destination);
        Directory.CreateDirectory(destinationDirectory);

        if (!System.IO.File.Exists(source) && !Directory.Exists(source))
        {
            return NotFound();
        }

        var target = ResolvePath(
            Path.Combine(
                Path.GetRelativePath(RootPath, destinationDirectory),
                Path.GetFileName(source)));

        if (System.IO.File.Exists(source))
        {
            System.IO.File.Move(source, target);
        }
        else
        {
            Directory.Move(source, target);
        }

        return NoContent();
    }

    [HttpDelete("delete")]
    public IActionResult Delete([FromQuery] string path)
    {
        var target = ResolvePath(path);

        if (string.Equals(target, RootPath, StringComparison.Ordinal))
        {
            return BadRequest("The root directory cannot be deleted.");
        }

        if (System.IO.File.Exists(target))
        {
            System.IO.File.Delete(target);
        }
        else if (Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
        else
        {
            return NotFound();
        }

        return NoContent();
    }

    private static void ValidateLeafName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)
            || name is "." or ".."
            || name.Contains('/')
            || name.Contains('\\')
            || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Invalid file or folder name.", nameof(name));
        }
    }

    public sealed record PathRequest(string Path);
    public sealed record RenameRequest(string Path, string NewName);
    public sealed record MoveRequest(string Path, string Destination);

    public sealed record FileEntry(
        string Path,
        string Name,
        bool IsDirectory,
        long? Size,
        DateTime LastModifiedUtc);
}
