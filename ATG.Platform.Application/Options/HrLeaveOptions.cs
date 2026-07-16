namespace ATG.Platform.Application.Options;

public class HrLeaveOptions
{
    public const string SectionName = "HrLeave";

    /// <summary>Dev/test: after HR review, skip deputy/head/supervising and go straight to GD.</summary>
    public bool ShortHoApprovalChain { get; set; }

    public string HoGeneralDirectorEmail { get; set; } = "liuzhiguang@atg.uz";

    /// <summary>Base URL for QR codes on leave PDFs, e.g. http://localhost:3000</summary>
    public string PublicAppBaseUrl { get; set; } = "http://localhost:3000";

    /// <summary>HO HR specialist responsible for business trip memoranda review.</summary>
    public string HoBusinessTripResponsibleEmail { get; set; } = "a.khamroev@atg.uz";

    /// <summary>HO HR specialist responsible for issuing leave orders (приказ) after GD approval.</summary>
    public string HoOrderResponsibleEmail { get; set; } = "a.khamroev@atg.uz";

    /// <summary>Word template file name for business trip orders (приказ), under Hr/Templates/.</summary>
    public string HoBusinessTripOrderTemplateFile { get; set; } = "BusinessTripOrder.docx";
}
