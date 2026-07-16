namespace ATG.Platform.Infrastructure.Dcs;

public static class InternationalContractsDirectForeignSteps
{
    public const int TotalSteps = 5;

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
            "Проект контракта и направление поставщику",
            "Draft contract & send to supplier",
            "Специалист готовит проект контракта и направляет поставщику/подрядчику.",
            "Specialist prepares the draft contract and sends it to the supplier/contractor.",
            null, null),

        new(4, false,
            "Согласование контракта в Портале",
            "Contract approval on the Portal",
            "Специалист готовит контракт с поставщиком и начинает согласование в Портале. Подписывают: инициирующий департамент, HO-CPROC, юридический департамент, зам. гендиректора по коммерции, первый зам. гендиректора, гендиректор.",
            "Specialist finalizes the supplier contract and starts Portal approval: initiator dept, HO-CPROC, legal, commercial deputy CEO, first deputy CEO, CEO.",
            null, null),

        new(5, false,
            "Визирование, регистрация и учёт контракта",
            "Endorsement, registration & accounting",
            "После подписания в Портале специалист визирует бумажный контракт; зам. начальника HO-CPROC и юрист визируют каждый лист и регистрируют. Контракт подписывает гендиректор, специалист ставит печать СП и сканирует. Подписанный контракт с ЛЗМ/ЛЗУ и техзаданием направляется в отдел управления контрактами и специалисту по аналитике HO-CPROC; в копию — ответственное лицо инициирующего департамента. Заявка исполнена отделом.",
            "After Portal signing: specialist endorses paper contract; HO-CPROC deputy and legal endorse each page and register. CEO signs, specialist applies company seal and scans. Signed contract with MR/SR and TA sent to Contracts Administration and HO-CPROC analytics; initiator responsible person in copy. Request completed by section.",
            null, null),
    ];
}
