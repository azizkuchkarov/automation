using ATG.Platform.Domain.Enums;

namespace ATG.Platform.Domain.Entities;

public class HrLeaveRequestItem
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public HrLeaveItemType Type { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? DaysCount { get; set; }
    public string? NoteRu { get; set; }
    public string? NoteEn { get; set; }
    public int SortOrder { get; set; }

    public HrLeaveRequestDetail Request { get; set; } = null!;
}
