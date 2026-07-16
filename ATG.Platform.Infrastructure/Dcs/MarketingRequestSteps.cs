namespace ATG.Platform.Infrastructure.Dcs;

public static class MarketingRequestSteps
{
    public const int TotalSteps = 8;

    public static IReadOnlyList<MarketingStepDefinition> Definitions { get; } =
    [
        new(1, false,
            "Назначение инженера-маркетолога",
            "Assign Marketing Engineer",
            "Руководитель отдела маркетинга назначает инженера из отдела маркетинга (HO-MKT-MKT) или возвращает заявку инициатору на доработку.",
            "Marketing section head assigns an engineer from the Marketing Section (HO-MKT-MKT) or returns the request to the initiator for revision.",
            null, null),

        new(2, true,
            "Изучение ТЗ, ЛЗМ/ЛЗУ и приложений",
            "Review TA, MR/SR and attachments",
            "Инженер-маркетолог изучает техническое задание, листы запроса материалов/услуг и приложенные документы.",
            "Marketing Engineer reviews the technical assignment, material/service requisitions and attached documents.",
            "Есть неясности в ТЗ? → Обратитесь к руководству департамента. После устранения — продолжите работу.",
            "Unclear points in TA? → Escalate to department management. Continue after resolution."),

        new(3, false,
            "Подготовка и рассылка RFQ",
            "RFQ preparation & distribution",
            "Подготовка RFQ, загрузка документа, публикация на tenderweek.com (инженер) и запрос в IT на сайт ATG. После закрытия всех каналов — «Выполнено».",
            "Prepare RFQ, upload the document, publish on tenderweek.com (engineer) and open IT request for ATG website. Mark complete when all channels are closed.",
            null, null),

        new(4, false,
            "Согласование технических параметров с инициатором",
            "Technical parameters coordination with Initiator",
            "Инженер-маркетолог добавляет коммерческие предложения (компания, тех. документ, цена). Инициатор согласовывает или отклоняет каждое предложение.",
            "Marketing Engineer adds commercial proposals (company, technical document, price). Initiator approves or rejects each proposal.",
            null, null),

        new(5, false,
            "Утверждённые коммерческие предложения",
            "Approved commercial proposals",
            "Список коммерческих предложений, согласованных инициатором. Проверьте и завершите шаг для перехода к плану закупки.",
            "Commercial proposals approved by the initiator. Review and complete the step to proceed to the procurement plan.",
            null, null),

        new(6, true,
            "Подготовка плана закупки",
            "Procurement plan preparation",
            "Подготовка плана закупки: определение метода закупки (в соответствии с законодательством), отправка полного пакета документов руководству департамента по email.",
            "Prepare procurement plan (Plan zakupa): define procurement method per legislation, email full document package to department management.",
            "Получены замечания руководства? → Внесите исправления и направьте повторно.",
            "Management feedback received? → Apply corrections and resubmit."),

        new(7, false,
            "Согласование плана закупки",
            "Procurement plan approval",
            "Согласование консолидированного плана закупки на этой странице. Локальная закупка: Первый зам. ген. директора + Ген. директор. Иные виды: члены закупочной комиссии + Ген. директор.",
            "Approve consolidated procurement plan inline on this page. Local procurement: First Deputy CEO + CEO. Other types: procurement commission members + CEO.",
            null, null),

        new(8, false,
            "Регистрация маркетингового процесса",
            "Marketing process registration",
            "Регистрация завершённого маркетингового процесса. После подтверждения заявка автоматически передаётся в Департамент по контрактам и закупкам.",
            "Register the completed marketing process. After confirmation the request is automatically forwarded to Contracts & Procurement.",
            null, null),
    ];
}

public record MarketingStepDefinition(
    int Number,
    bool HasBranch,
    string TitleRu,
    string TitleEn,
    string HintRu,
    string HintEn,
    string? BranchHintRu,
    string? BranchHintEn);
