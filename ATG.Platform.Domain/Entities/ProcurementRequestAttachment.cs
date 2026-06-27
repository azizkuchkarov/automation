using ATG.Platform.Domain.Enums;



namespace ATG.Platform.Domain.Entities;



public class ProcurementRequestAttachment

{

    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public ProcurementAttachmentKind Kind { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string? StorageKey { get; set; }
    public Guid UploadedById { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;



    public ProcurementRequestDetail Request { get; set; } = null!;

    public User UploadedBy { get; set; } = null!;

}


