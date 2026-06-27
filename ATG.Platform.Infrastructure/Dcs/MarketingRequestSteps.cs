namespace ATG.Platform.Infrastructure.Dcs;

public static class MarketingRequestSteps
{
    public const int TotalSteps = 11;

    public static IReadOnlyList<MarketingStepDefinition> Definitions { get; } =
    [
        new(1, true,
            "Принятие заявки и назначение инженера-маркетолога",
            "Accept request and assign Marketing Engineer",
            "Руководитель отдела маркетинга принимает заявку (после согласования ЛЗМ/ЛЗУ) и назначает инженера-маркетолога.",
            "Marketing section head accepts the request (after MR/SR approval) and assigns a Marketing Engineer.",
            null, null),

        new(2, true,
            "Изучение ТЗ, ЛЗМ/ЛЗУ и приложений",
            "Review TA, MR/SR and attachments",
            "Инженер-маркетолог изучает техническое задание, листы запроса материалов/услуг и приложенные документы.",
            "Marketing Engineer reviews the technical assignment, material/service requisitions and attached documents.",
            "Есть неясности в ТЗ? → Обратитесь к руководству департамента. После устранения — продолжите работу.",
            "Unclear points in TA? → Escalate to department management. Continue after resolution."),

        new(3, false,
            "Подготовка RFQ",
            "RFQ preparation",
            "Подготовка запроса коммерческого предложения (RFQ). ТЗ прилагается: подписи и ФИО без указания — только подпись утверждающего на первой странице.",
            "Prepare Request for Quotation (RFQ). Attach TA: no signatures/names except approver signature on the first page.",
            null, null),

        new(4, false,
            "Рассылка RFQ",
            "RFQ distribution",
            "Одновременная рассылка RFQ по всем каналам: официальный сайт ATG, tenderweek.com, завод-изготовитель (вендор), дистрибьютор, открытые источники.",
            "Distribute RFQ simultaneously: ATG official website, tenderweek.com, manufacturer (vendor), distributor, open sources.",
            null, null),

        new(5, true,
            "Ожидание ответов",
            "Await responses",
            "Ожидание ответов от поставщиков (1–2 рабочих дня).",
            "Await supplier responses (1–2 business days).",
            "Ответ не получен? → Повторная отправка email и телефонный звонок, продолжение ожидания.",
            "No response? → Resend email and follow up by phone, continue waiting."),

        new(6, true,
            "Проверка коммерческих предложений",
            "Evaluate commercial proposals",
            "Проверка поступивших КП на соответствие требованиям ТЗ.",
            "Verify received commercial proposals (KP) against TA requirements.",
            "КП не соответствует ТЗ? → Переговоры с поставщиком (ошибка в ТЗ, устаревшая модель, аналог). Параллельно продолжайте маркетинг.",
            "KP does not match TA? → Negotiate with supplier (TA error, outdated model, analog). Continue parallel marketing."),

        new(7, true,
            "Подготовка плана закупки",
            "Procurement plan preparation",
            "Подготовка плана закупки: определение метода закупки (в соответствии с законодательством), отправка полного пакета документов руководству департамента по email.",
            "Prepare procurement plan (Plan zakupa): define procurement method per legislation, email full document package to department management.",
            "Получены замечания руководства? → Внесите исправления и направьте повторно.",
            "Management feedback received? → Apply corrections and resubmit."),

        new(8, false,
            "Направление на согласование через портал",
            "Portal submission for approval",
            "Направление консолидированного плана закупки на согласование через портал. Локальная закупка: Первый зам. ген. директора + Ген. директор. Иные виды: все члены закупочной комиссии + Ген. директор.",
            "Submit consolidated procurement plan for portal approval. Local procurement: First Deputy CEO + CEO. Other types: full procurement commission + CEO.",
            null, null),

        new(9, true,
            "Мониторинг согласования на портале",
            "Portal approval monitoring",
            "Мониторинг статуса согласования плана закупки на портале.",
            "Monitor procurement plan approval status on the portal.",
            "Нет согласования в течение 2 рабочих дней? → Уведомите руководство департамента для ускорения.",
            "Not approved within 2 business days? → Notify department management to expedite."),

        new(10, false,
            "Регистрация утверждённого плана",
            "Approved plan registration",
            "Направление утверждённого плана закупки в департамент делопроизводства (исполнения) для регистрации.",
            "Send approved procurement plan to Records / Execution department for registration.",
            null, null),

        new(11, false,
            "Маркетинговый процесс завершён",
            "Marketing process completed",
            "Маркетинговый процесс завершён. Заявка готова к передаче в Департамент по контрактам и закупкам.",
            "Marketing process completed. Request is ready for handoff to Contracts & Procurement Department.",
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
