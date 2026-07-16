namespace ATG.Platform.Infrastructure.Dcs;

public static class InternationalContractsTenderSteps
{
    public const int TotalSteps = 15;

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
            null, null),

        new(4, false,
            "Подписание закупочной документации",
            "Procurement documentation signing",
            "Закупочную документацию подписывает специалист отдела международных закупок и заместитель начальника Департамента по контрактам и закупкам, затем передают на подпись Генеральному директору.",
            "International section specialist and Deputy Head of Contracts & Procurement sign procurement documentation, then forward to the CEO for signature.",
            null, null),

        new(5, true,
            "Экспертиза в ГУП «Центр экспертизы импортных контрактов»",
            "Expertise at Import Contracts Expertise Center",
            "Специалист сканирует подписанную закупочную документацию и направляет вместе с ЛЗМ/ЛЗУ, техзаданием, планом отбора и проектами контрактов в ГУП «Центр экспертизы импортных контрактов» для получения заключения.",
            "Specialist scans signed procurement documentation and sends it with MR/SR, technical assignment, selection plan and draft contracts to the Import Contracts Expertise Center for a conclusion.",
            "5.1 Замечания Центра → ответ совместно с инициатором. 5.2 Положительное заключение → пакет секретарю комиссии.",
            "5.1 Center remarks → response with initiator. 5.2 Positive conclusion → package to commission secretary."),

        new(6, false,
            "План-график на xarid.uzex.uz",
            "Procurement schedule on xarid.uzex.uz",
            "Параллельно специалист вносит план-график закупки на информационный портал xarid.uzex.uz.",
            "In parallel, the specialist enters the procurement schedule on xarid.uzex.uz.",
            null, null),

        new(7, false,
            "Внесение данных по тендеру на портал госзакупок",
            "Tender data entry on public procurement portal",
            "Секретарь закупочной комиссии вносит данные по тендеру на портале государственных закупок.",
            "Procurement commission secretary enters tender data on the public procurement portal.",
            null, null),

        new(8, false,
            "Подписание объявления тендера членами комиссии",
            "Commission members sign tender announcement",
            "После прохождения модерации объявления на тендер подписывается членами комиссии на портале.",
            "After moderation, the tender announcement is signed by commission members on the portal.",
            null, null),

        new(9, false,
            "Вопросы участников в период объявления тендера",
            "Participant questions during tender announcement",
            "В период объявления тендера участники задают вопросы через портал. Секретарь направляет вопросы инициатору или специалисту отдела в зависимости от предмета.",
            "During the tender announcement period participants submit questions via the portal. The secretary routes them to the initiator or section specialist as appropriate.",
            null, null),

        new(10, true,
            "Итоги срока объявления тендера",
            "Tender announcement period outcome",
            "После окончания срока: если никто не подал предложение или только один участник — тендер несостоявшийся (переобъявление). Если два и более предложений — секретарь загружает документы и создаёт группу в Teams для оценочной группы.",
            "After the deadline: no bids or single bidder — tender failed (re-announce). Two or more bids — secretary uploads documents and creates a Teams group for the evaluation panel.",
            "10.1 Тендер несостоявшийся → протокол портала членам комиссии для согласия на переобъявление.",
            "10.1 Tender failed → portal protocol to commission for re-announcement approval."),

        new(11, false,
            "Оценка квалификационной и технической части",
            "Qualification & technical evaluation",
            "Члены оценочной группы вносят изменения в таблицы оценки квалификационной и технической части предложений участников.",
            "Evaluation panel members update qualification and technical scoring tables for participant proposals.",
            null, null),

        new(12, true,
            "Проект отчёта и итоги оценки тендера",
            "Draft evaluation report & tender outcomes",
            "Специалист готовит проект отчёта с комментариями оценочной группы, выкладывает в Teams. После одобрения собирает подписи и передаёт секретарю для рассмотрения комиссии.",
            "Specialist prepares draft report with panel comments, posts in Teams. After approval collects signatures and forwards to secretary for commission review.",
            "12.1 Только один или никто → тендер несостоявшийся. 12.2 Два и более → портал выдаёт протокол комиссии с победителем.",
            "12.1 One or zero qualified → tender failed. 12.2 Two or more → portal issues commission protocol with winner."),

        new(13, false,
            "Подготовка и согласование контракта",
            "Contract preparation & portal approval",
            "Специалист готовит контракт с поставщиком и начинает согласование в Портале. Подписывают: инициирующий департамент, HO-CPROC, юридический департамент, зам. гендиректора по коммерции, первый зам. гендиректора, гендиректор.",
            "Specialist prepares supplier contract and starts portal approval: initiator dept, HO-CPROC, legal, commercial deputy CEO, first deputy CEO, CEO.",
            null, null),

        new(14, false,
            "Визирование и подписание бумажного контракта",
            "Paper contract endorsement & signing",
            "После подписания в Портале специалист визирует бумажный контракт; зам. начальника HO-CPROC и юрист визируют каждый лист и регистрируют. Контракт подписывает гендиректор, специалист ставит печать СП и сканирует.",
            "After portal signing: specialist endorses paper contract; HO-CPROC deputy and legal endorse each page and register. CEO signs, specialist applies company seal and scans.",
            null, null),

        new(15, false,
            "Регистрация и учёт контракта",
            "Contract registration & accounting",
            "Подписанный контракт с ЛЗМ/ЛЗУ и техзаданием направляется в отдел управления контрактами и специалисту по аналитике HO-CPROC. В копию — ответственное лицо инициирующего департамента. Заявка исполнена отделом.",
            "Signed contract with MR/SR and TA sent to Contracts Administration and HO-CPROC analytics. Initiator responsible person in copy. Request completed by section.",
            null, null),
    ];
}
