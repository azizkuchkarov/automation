using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATG.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingRecordsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "marketing_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PortalNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    RegisteredDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InitiatorDepartment = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    InitiatorFullName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ReceivedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeadlineBaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    RequestCategory = table.Column<int>(type: "integer", nullable: true),
                    DeadlineWorkingDays = table.Column<int>(type: "integer", nullable: true),
                    DeadlineDate = table.Column<DateOnly>(type: "date", nullable: true),
                    MarketingExecutorId = table.Column<Guid>(type: "uuid", nullable: true),
                    AssignedByManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    HandoverDate = table.Column<DateOnly>(type: "date", nullable: true),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ProcurementMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    StrategyNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    StrategyNumberManual = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    BudgetCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LegalBasis = table.Column<string>(type: "text", nullable: true),
                    RfqPreparedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RfqPublishedAtgSite = table.Column<bool>(type: "boolean", nullable: false),
                    RfqPublishedTenderweek = table.Column<bool>(type: "boolean", nullable: false),
                    RfqSentToVendor = table.Column<bool>(type: "boolean", nullable: false),
                    RfqSentToDistributor = table.Column<bool>(type: "boolean", nullable: false),
                    RfqOpenSearchDone = table.Column<bool>(type: "boolean", nullable: false),
                    TzIssueFound = table.Column<bool>(type: "boolean", nullable: false),
                    TzIssueDescription = table.Column<string>(type: "text", nullable: true),
                    TzIssueResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlanPreparedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlanSentToManagementAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlanSubmittedToPortalAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlanApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PlanRegisteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PortalApprovalStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PortalApprovalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PortalBudgetNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketing_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketing_records_procurement_request_details_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "procurement_request_details",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_marketing_records_users_AssignedByManagerId",
                        column: x => x.AssignedByManagerId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_marketing_records_users_MarketingExecutorId",
                        column: x => x.MarketingExecutorId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "marketing_offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OfferAmount = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    VatIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryIncluded = table.Column<bool>(type: "boolean", nullable: false),
                    WarrantyTerms = table.Column<string>(type: "text", nullable: true),
                    OfferDate = table.Column<DateOnly>(type: "date", nullable: true),
                    OfferValidityDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ContactInfo = table.Column<string>(type: "text", nullable: true),
                    MeetsTzRequirements = table.Column<bool>(type: "boolean", nullable: true),
                    RejectionReason = table.Column<string>(type: "text", nullable: true),
                    IsAffiliated = table.Column<bool>(type: "boolean", nullable: false),
                    AffiliationNote = table.Column<string>(type: "text", nullable: true),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AttachmentKey = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketing_offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketing_offers_marketing_records_MarketingRecordId",
                        column: x => x.MarketingRecordId,
                        principalTable: "marketing_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketing_procurement_plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    ProcurementMethod = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartPrice = table.Column<decimal>(type: "numeric(15,2)", precision: 15, scale: 2, nullable: true),
                    StartPriceCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    VatConsidered = table.Column<bool>(type: "boolean", nullable: false),
                    Incoterms = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CompetitionCriteria = table.Column<string>(type: "text", nullable: true),
                    EvaluationGroupMembers = table.Column<string>(type: "text", nullable: true),
                    NdsNote = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    RejectionNotes = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AttachmentKey = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketing_procurement_plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketing_procurement_plans_marketing_records_MarketingReco~",
                        column: x => x.MarketingRecordId,
                        principalTable: "marketing_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rfq_dispatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    DispatchType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecipientName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RecipientEmail = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    RecipientPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResponseReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FollowupSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FollowupPhoneCalled = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rfq_dispatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rfq_dispatches_marketing_records_MarketingRecordId",
                        column: x => x.MarketingRecordId,
                        principalTable: "marketing_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketing_portal_approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MarketingRecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProcurementPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BudgetNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReminderSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_marketing_portal_approvals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketing_portal_approvals_marketing_procurement_plans_Proc~",
                        column: x => x.ProcurementPlanId,
                        principalTable: "marketing_procurement_plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_marketing_portal_approvals_marketing_records_MarketingRecor~",
                        column: x => x.MarketingRecordId,
                        principalTable: "marketing_records",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_marketing_offers_MarketingRecordId",
                table: "marketing_offers",
                column: "MarketingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_portal_approvals_MarketingRecordId",
                table: "marketing_portal_approvals",
                column: "MarketingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_portal_approvals_ProcurementPlanId",
                table: "marketing_portal_approvals",
                column: "ProcurementPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_procurement_plans_MarketingRecordId",
                table: "marketing_procurement_plans",
                column: "MarketingRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_records_AssignedByManagerId",
                table: "marketing_records",
                column: "AssignedByManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_records_DeadlineDate",
                table: "marketing_records",
                column: "DeadlineDate");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_records_DocumentId",
                table: "marketing_records",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketing_records_MarketingExecutorId",
                table: "marketing_records",
                column: "MarketingExecutorId");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_records_PortalNumber",
                table: "marketing_records",
                column: "PortalNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_marketing_records_RequestCategory",
                table: "marketing_records",
                column: "RequestCategory");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_records_Status",
                table: "marketing_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rfq_dispatches_MarketingRecordId",
                table: "rfq_dispatches",
                column: "MarketingRecordId");

            migrationBuilder.Sql(
                """
                INSERT INTO marketing_records (
                    "Id", "DocumentId", "PortalNumber", "RegisteredDate", "InitiatorDepartment",
                    "InitiatorFullName", "ReceivedDate", "DeadlineBaseDate", "RequestTitle",
                    "Status", "BudgetCurrency", "CreatedAt", "UpdatedAt"
                )
                SELECT
                    gen_random_uuid(),
                    p."DocumentId",
                    d."Number",
                    CASE WHEN d."RegisteredAt" IS NOT NULL THEN CAST(d."RegisteredAt" AS date) ELSE NULL END,
                    dept."Name",
                    u."LastName" || ' ' || u."FirstName",
                    CAST(COALESCE(d."RegisteredAt", d."CreatedAt") AS date),
                    CAST(COALESCE(d."RegisteredAt", d."CreatedAt") AS date),
                    d."Title",
                    'WaitingExecutor',
                    'UZS',
                    NOW(),
                    NOW()
                FROM procurement_request_details p
                INNER JOIN documents d ON d."Id" = p."DocumentId"
                LEFT JOIN users u ON u."Id" = p."InitiatorId"
                LEFT JOIN departments dept ON dept."Id" = p."InitiatorDepartmentId"
                WHERE p."Phase" = 'Marketing'
                  AND NOT EXISTS (SELECT 1 FROM marketing_records m WHERE m."DocumentId" = p."DocumentId");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketing_offers");

            migrationBuilder.DropTable(
                name: "marketing_portal_approvals");

            migrationBuilder.DropTable(
                name: "rfq_dispatches");

            migrationBuilder.DropTable(
                name: "marketing_procurement_plans");

            migrationBuilder.DropTable(
                name: "marketing_records");
        }
    }
}
