namespace ATG.Platform.Infrastructure.Dcs;

public static class InternationalContractsSbpSteps
{
    public const int TotalSteps = 15;

    /// <summary>Steps 1–2 are fulfilled by department routing and engineer assignment.</summary>
    public const int FirstOperationalStep = 3;

    public static IReadOnlyList<ContractsIntStepDefinition> Definitions { get; } =
    [
        new(1, false,
            "Передача ЛЗМ/ЛЗУ начальнику отдела международных закупок",
            "Handover of MR/SR to International Procurement Section head",
            "Начальник Департамента по контрактам и закупкам расписывает на начальника отдела международных закупок ЛЗМ/ЛЗУ.",
            "Contracts & Procurement Department head endorses MR/SR to the International Procurement Section head.",
            null, null),

        new(2, false,
            "Распределение заявки специалисту",
            "Assignment to section specialist",
            "Начальник отдела международных закупок распределяет заявку специалисту отдела или исполняет самостоятельно.",
            "International Procurement Section head assigns the request to a section specialist or executes personally.",
            null, null),

        new(3, false,
            "Подготовка закупочной документации и проектов контрактов",
            "Procurement documentation & draft contracts",
            "Специалист готовит закупочную документацию и проекты контрактов для резидентов и нерезидентов. При подготовке взаимодействует с секретарём закупочной комиссии (HO-MKT).",
            "Specialist prepares procurement documentation and draft contracts for residents and non-residents, coordinating with the procurement commission secretary (HO-MKT).",
            null, null,
            RequiresUpload: true),

        new(4, false,
            "Подписание закупочной документации",
            "Procurement documentation signing",
            "Закупочную документацию подписывает специалист отдела международных закупок и заместитель начальника Департамента по контрактам и закупкам, затем передают на подпись Генеральному директору.",
            "International section specialist and Deputy Head of Contracts & Procurement sign procurement documentation, then forward to the CEO for signature.",
            null, null,
            RequiresApprovers: true),

        new(5, false,
            "Направление документации секретарю комиссии",
            "Send signed documentation to commission secretary",
            "Специалист сканирует подписанную закупочную документацию и направляет вместе с ЛЗМ/ЛЗУ, техзаданием, планом отбора и проектами контрактов секретарю закупочной комиссии (Тендерный секретариат).",
            "Specialist scans signed procurement documentation and sends it with MR/SR, technical assignment, selection plan and draft contracts to the Tender Secretariat.",
            null, null,
            RequiresSecretariat: true),

        new(6, false,
            "План-график на xarid.uzex.uz",
            "Procurement schedule on xarid.uzex.uz",
            "Параллельно специалист вносит план-график закупки на информационный портал xarid.uzex.uz.",
            "In parallel, the specialist enters the procurement schedule on xarid.uzex.uz.",
            null, null),

        new(7, false,
            "Внесение данных на портал госзакупок",
            "Data entry on public procurement portal",
            "Секретарь закупочной комиссии вносит данные по отбору на портале государственных закупок.",
            "Procurement commission secretary enters selection data on the public procurement portal.",
            null, null),

        new(8, false,
            "Подписание объявления членами комиссии",
            "Commission members sign the announcement",
            "После прохождения модерации объявления об отборе наилучших предложений подписывается членами комиссии на портале.",
            "After moderation, the SBP announcement is signed by commission members on the portal.",
            null, null,
            RequiresApprovers: true),

        new(9, false,
            "Вопросы участников в период объявления",
            "Participant questions during announcement",
            "В период объявления участники задают вопросы через портал. Секретарь направляет вопросы инициатору или специалисту отдела в зависимости от предмета.",
            "During the announcement period participants submit questions via the portal. The secretary routes them to the initiator or section specialist as appropriate.",
            null, null),

        new(10, true,
            "Итоги срока объявления отбора",
            "Announcement period outcome",
            "После окончания срока объявления: если никто не подал предложение или только один участник — отбор несостоявшийся (переобъявление). Если два и более предложений — секретарь загружает документы и создаёт группу в Teams для оценочной группы.",
            "After the announcement deadline: no bids or single bidder — selection failed (re-announce). Two or more bids — secretary uploads documents and creates a Teams group for the evaluation panel.",
            "Отбор несостоявшийся? → Секретарь направляет протокол портала членам комиссии для согласия на переобъявление.",
            "Selection failed? → Secretary sends portal protocol to commission members for re-announcement approval."),

        new(11, false,
            "Оценка квалификационной и технической части",
            "Qualification & technical evaluation",
            "Члены оценочной группы вносят изменения в таблицы оценки квалификационной и технической части предложений участников.",
            "Evaluation panel members update qualification and technical scoring tables for participant proposals.",
            null, null),

        new(12, true,
            "Проект отчёта и итоги оценки",
            "Draft evaluation report & outcomes",
            "Специалист готовит проект отчёта с комментариями оценочной группы, выкладывает в Teams. После одобрения собирает подписи и передаёт секретарю.",
            "Specialist prepares draft report with panel comments, posts in Teams. After approval collects signatures and forwards to secretary.",
            "Оценку прошёл только один участник или никто? → Отбор несостоявшийся; возможно переобъявление или пересмотр ТЗ.",
            "Only one or zero qualified? → Selection failed; re-announce or revise technical assignment."),

        new(13, false,
            "Подготовка и согласование контракта",
            "Contract preparation & portal approval",
            "Специалист готовит контракт с поставщиком и начинает согласование в Портале. Подписывают: инициирующий департамент, HO-CPROC, юридический департамент, зам. гендиректора по коммерции, первый зам. гендиректора, гендиректор.",
            "Specialist prepares supplier contract and starts portal approval chain: initiator dept, HO-CPROC, legal, commercial deputy CEO, first deputy CEO, CEO.",
            null, null,
            RequiresApprovers: true),

        new(14, false,
            "Визирование и подписание бумажного контракта",
            "Paper contract endorsement & signing",
            "После подписания в Портале специалист визирует бумажный контракт; зам. начальника HO-CPROC и юрист визируют каждый лист и регистрируют в книге. Контракт подписывает гендиректор, специалист ставит печать СП и сканирует.",
            "After portal signing: specialist endorses paper contract; HO-CPROC deputy and legal endorse each page and register. CEO signs, specialist applies company seal and scans.",
            null, null,
            RequiresUpload: true),

        new(15, false,
            "Регистрация и учёт контракта",
            "Contract registration & accounting",
            "Подписанный контракт загружается и регистрируется. Заявка передаётся в Payment.",
            "Signed contract is uploaded and registered. Request moves to Payment.",
            null, null,
            RequiresUpload: true,
            RequiresRegistration: true),
    ];
}

public record ContractsIntStepDefinition(
    int Number,
    bool HasBranch,
    string TitleRu,
    string TitleEn,
    string HintRu,
    string HintEn,
    string? BranchHintRu,
    string? BranchHintEn,
    bool RequiresUpload = false,
    bool RequiresApprovers = false,
    bool RequiresSecretariat = false,
    bool RequiresRegistration = false);
