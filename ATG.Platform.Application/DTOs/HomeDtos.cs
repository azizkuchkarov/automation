namespace ATG.Platform.Application.DTOs;

public record HomeModuleCountsDto(
    int Admin,
    int Automation,
    int ItAutomation,
    int HelpDesk,
    int Hr,
    int Tasks);
