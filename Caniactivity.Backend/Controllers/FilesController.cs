using Caniactivity.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text.Json;

namespace Caniactivity.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IHostEnvironment _hostEnvironment;

        public FilesController(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        string PhotosDirectoryPath
        {
            get { return Path.Combine(_hostEnvironment.ContentRootPath, "Photos"); }
        }

        [HttpPost(Name = "Post- FileManagementApi")]
        [DisableFormValueModelBinding]
        [Authorize]
        public async Task<FilesResponse> FileSystemPost()
        {
            try
            {
                if (MultipartRequestManager.IsMultipartContentType(Request.ContentType))
                {
                    IFormCollection form = await Request.ReadFormAsync();
                    var command = form["command"];
                    if (command == "UploadChunk")
                    {
                        var filename = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(form["arguments"])["chunkMetadata"].ToString()
                            )["FileName"].ToString();

                            var count = 0;
                            var totalSize = 0L;
                            // find the boundary
                            var boundary = MultipartRequestManager.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType));
                            // use boundary to iterator through the multipart section
                            var reader = new MultipartReader(boundary, Request.Body);
                            var section = await reader.ReadNextSectionAsync();
                            do
                            {
                                ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                                if (!MultipartRequestManager.HasFileContentDisposition(contentDisposition))
                                {
                                    section = await reader.ReadNextSectionAsync();
                                    continue;
                                }
                                totalSize += await SaveFileAsync(section, filename);

                                count++;
                                section = await reader.ReadNextSectionAsync();
                            } while (section != null);

                            return new FilesResponse()
                            {
                                Success = true,
                                ErrorCode = null,
                                ErrorText = string.Empty,
                                Result = EnumerateFiles()
                            };
                    }
                }
                else if (Request.ContentType.Contains("text/plain"))
                {
                    if (Request.Query["command"][0] == "Remove")
                    {
                        var arguments = Request.Query["arguments"][0];
                        Dictionary<string, object> args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments);
                        ((JsonElement)args["pathInfo"]).Deserialize<List<object>>()
                            .ForEach(w =>
                            {
                                Dictionary<string, string> file = ((JsonElement)w).Deserialize<Dictionary<string, string>>();
                                System.IO.File.Delete(_hostEnvironment.ContentRootPath + file["key"]);
                            });
                    }
                }

            }
            catch (Exception exception)
            {
                return new FilesResponse()
                {
                    Success = false,
                    ErrorCode = exception.Message,
                    ErrorText = exception.Message,
                    Result = new List<FileInDirectory>()
                };
            }

            return new FilesResponse() { 
                Success = true,
                ErrorCode = null,
                ErrorText = string.Empty,
                Result = EnumerateFiles()
            };
        }

        [HttpGet(Name = "Get-FileManagementApi")]
        //[Authorize]
        public async Task<FilesResponse> FileSystemGet(string command, string arguments)
        {
            if (command == "GetDirContents")
            {
                return new FilesResponse()
                {
                    Success = true,
                    ErrorCode = null,
                    ErrorText = string.Empty,
                    Result = EnumerateFiles()
                };
            }

            return new FilesResponse()
            {
                Success = false,
                ErrorCode = "NotImplemented",
                ErrorText = "NotImplemented"
            };
        }

        private List<FileInDirectory> EnumerateFiles()
        {
            return Directory.EnumerateFiles(PhotosDirectoryPath)
                .Select(w =>
                {
                    FileInfo info = new System.IO.FileInfo(w);
                    return new FileInDirectory()
                    {
                        Key = w.Replace(_hostEnvironment.ContentRootPath, ""),
                        Name = info.Name,
                        DateModified = info.LastWriteTimeUtc.ToString(),
                        IsDirectory = false,
                        Size = info.Length,
                        HasSubdirectory = false
                    };
                }).ToList();
        }

        private async Task<long> SaveFileAsync(MultipartSection section, string fileName)
        {
            //subDirectory ??= string.Empty;
            //var target = Path.Combine("{root}", subDirectory);
            Directory.CreateDirectory(PhotosDirectoryPath);

            var fileSection = section.AsFileSection();

            var filePath = Path.Combine(PhotosDirectoryPath, fileName);

            using var stream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None, 1024);
            await fileSection.FileStream.CopyToAsync(stream);

            return fileSection.FileStream.Length;
        }
    }

    public class FilesResponse
    {
        public bool Success { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorText { get; set; } = string.Empty;
        public List<FileInDirectory> Result { get; set; } = new List<FileInDirectory>();
    }

    public class FileInDirectory
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string DateModified { get; set; } // ISO string UTC
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public bool HasSubdirectory { get; set; }
    }
}
