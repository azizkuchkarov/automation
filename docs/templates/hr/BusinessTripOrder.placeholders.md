# Business trip order template (приказ)

The order document is generated programmatically as a bilingual two-column Word file.
The skeleton `BusinessTripOrder.docx` is only used for styles/package metadata; body content is built in code.

## Flow

1. After full approval (GD E-IMZO), memoranda move to phase `OrderPending` and appear in **a.khamroev**'s order queue.
2. HR selects **1–N memoranda** and clicks **Issue order**.
3. One shared order number (`HBO-YYYY-NNN`) and one `.docx` are created.

## Section numbering

| Selected memos | Trip sections | Accounting | Report |
|---|---|---|---|
| 1 | §1 | §2 | §3 |
| 2 | §1, §2 | §3 | §4 |
| 3 | §1, §2, §3 | §4 | §5 |

Within each memorandum:

- **1 traveler** → single-employee § text
- **2+ travelers** → group § text with sub-items `N.1`, `N.2`, …

## Auto-filled fields

| Field | Source |
|---|---|
| Order date (header) | Date when HR issues the order |
| Purpose | `PurposeRu` / `PurposeEn` from memorandum |
| Travelers | `Travelers` list (name + position) |
| Place, dates, days | Memorandum fields |
| Basis (Основание) | One line per memo: department (genitive) + memo `RequestDate` |

## Department genitive (родительный падеж)

Set `NameGenitive` on the department record (DB column `name_genitive`).
If empty, nominative `Name` is used as fallback.

Example: «Служебная записка (отдела автоматизации) от 08.07.2026 г.»
