using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using LivestockManager.Domain.Enums;

namespace LivestockManager.Web.Models.DocumentViewModels;

public class UploadDocumentViewModel
{
    public string? EntityTypeStr { get; set; }

    public string? EntityIdStr { get; set; }

    public DocumentType DocumentType { get; set; }

    public List<IFormFile>? Files { get; set; }
}
