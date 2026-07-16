"use client";

import { useEffect, useState } from "react";
import { CreditCard, Globe2, MapPin } from "lucide-react";
import type { useTranslations } from "next-intl";
import {
  ContractsDomProcurementVariant,
  ContractsIntProcurementVariant,
  ContractsProcurementSectionType,
  ProcurementRequest,
  ProcurementRequestUser,
} from "@/lib/procurementRequest";
import { ContractsDepartmentRoutingPanel } from "@/components/dcs/ContractsDepartmentRoutingPanel";
import { ContractsWorkflowPanel } from "@/components/dcs/ContractsWorkflowPanel";
import { InternationalContractsWorkflowPanel } from "@/components/dcs/InternationalContractsWorkflowPanel";
import { DomesticContractsWorkflowPanel } from "@/components/dcs/DomesticContractsWorkflowPanel";
import { cn } from "@/lib/utils";

export type ContractsInnerTab = "Domestic" | "International" | "Payment";

interface Props {
  req: ProcurementRequest;
  locale: string;
  t: ReturnType<typeof useTranslations>;
  acting: boolean;
  isLivePhase: boolean;
  contractsPerms?: ProcurementRequest["contractsPermissions"];
  contractsWorkers: ProcurementRequestUser[];
  selectedEngineer: string;
  setSelectedEngineer: (v: string) => void;
  selectedContractsSection: ContractsProcurementSectionType | "";
  setSelectedContractsSection: (v: ContractsProcurementSectionType | "") => void;
  contractsComment: string;
  setContractsComment: (v: string) => void;
  onRouteSection: (section?: ContractsProcurementSectionType) => void;
  onAssign: () => void;
  onAccept: () => void;
  selectedIntVariant: ContractsIntProcurementVariant | "";
  setSelectedIntVariant: (v: ContractsIntProcurementVariant | "") => void;
  approverCandidates: ProcurementRequestUser[];
  selectedApproverIds: string[];
  setSelectedApproverIds: (ids: string[]) => void;
  onSelectIntVariant: () => void;
  onCompleteContractsIntStep: (step: number) => void;
  onUploadStepFile: (step: number, fileName: string, storageKey: string) => void;
  onSubmitApprovers: (step: number) => void;
  onDecideApproval: (step: number, approve: boolean) => void;
  onSendToSecretariat: (step: number) => void;
  selectedDomVariant: ContractsDomProcurementVariant | "";
  setSelectedDomVariant: (v: ContractsDomProcurementVariant | "") => void;
  onSelectDomVariant: () => void;
  onCompleteContractsDomStep: (step: number) => void;
  onScheduleDomStep: (step: number, date: string) => void;
  onUploadDomStepFile: (step: number, fileName: string, storageKey: string) => void;
  onSubmitDomApprovers: (step: number) => void;
  onDecideDomApproval: (step: number, approve: boolean) => void;
  onSendToContractsAdmin: (step: number) => void;
  onReturnDomToMarketing: (step: number) => void;
  onRollbackDomStep: (step: number) => void;
}

function defaultTab(req: ProcurementRequest): ContractsInnerTab {
  if (req.contractsProcurementSection === "Domestic") return "Domestic";
  if (req.contractsProcurementSection === "International") return "International";
  return "Domestic";
}

export function ContractsPhaseTabs(props: Props) {
  const { req, t, isLivePhase, contractsPerms } = props;
  const needsDeptRouting =
    req.phase === "Contracts" &&
    req.contractsSubPhase === "Pending" &&
    !req.contractsProcurementSection;

  const [tab, setTab] = useState<ContractsInnerTab>(() => defaultTab(req));

  useEffect(() => {
    if (req.contractsProcurementSection === "Domestic") setTab("Domestic");
    else if (req.contractsProcurementSection === "International") setTab("International");
  }, [req.contractsProcurementSection]);

  // Step 1: Contracts Department Head must choose Local or International.
  if (needsDeptRouting) {
    return (
      <ContractsDepartmentRoutingPanel
        req={req}
        locale={props.locale}
        t={t}
        acting={props.acting}
        canRoute={Boolean(isLivePhase && contractsPerms?.canRouteSection)}
        selectedSection={props.selectedContractsSection}
        setSelectedSection={props.setSelectedContractsSection}
        stepComment={props.contractsComment}
        setStepComment={props.setContractsComment}
        onRouteSection={props.onRouteSection}
      />
    );
  }

  const tabs: {
    id: ContractsInnerTab;
    label: string;
    icon: typeof MapPin;
    active: boolean;
  }[] = [
    {
      id: "Domestic",
      label: t("contractsTabs.domestic"),
      icon: MapPin,
      active: req.contractsProcurementSection === "Domestic",
    },
    {
      id: "International",
      label: t("contractsTabs.international"),
      icon: Globe2,
      active: req.contractsProcurementSection === "International",
    },
    {
      id: "Payment",
      label: t("contractsTabs.payment"),
      icon: CreditCard,
      active: false,
    },
  ];

  return (
    <div className="space-y-4">
      <div className="rounded-2xl border border-indigo-500/20 bg-surface shadow-sm overflow-hidden">
        <div className="flex gap-1 overflow-x-auto border-b border-indigo-500/10 bg-indigo-500/[0.04] p-2">
          {tabs.map(({ id, label, icon: Icon, active }) => (
            <button
              key={id}
              type="button"
              onClick={() => setTab(id)}
              className={cn(
                "inline-flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold whitespace-nowrap transition-all",
                tab === id
                  ? "bg-white text-indigo-700 shadow-sm ring-1 ring-indigo-500/20 dark:bg-white/[0.08] dark:text-indigo-300"
                  : "text-foreground/50 hover:bg-white/60 hover:text-foreground/80 dark:hover:bg-white/[0.04]",
              )}
            >
              <Icon size={15} />
              {label}
              {active && (
                <span
                  className="h-1.5 w-1.5 rounded-full bg-emerald-500"
                  title={t("contractsTabs.activeSection")}
                />
              )}
            </button>
          ))}
        </div>

        <div className="p-1">
          {tab === "Domestic" && (
            <SectionTabBody {...props} section="Domestic" isLivePhase={isLivePhase} />
          )}
          {tab === "International" && (
            <SectionTabBody {...props} section="International" isLivePhase={isLivePhase} />
          )}
          {tab === "Payment" && <PaymentTabBody t={t} />}
        </div>
      </div>
    </div>
  );
}

function SectionTabBody({
  section,
  req,
  locale,
  t,
  acting,
  isLivePhase,
  contractsPerms,
  contractsWorkers,
  selectedEngineer,
  setSelectedEngineer,
  selectedContractsSection,
  setSelectedContractsSection,
  contractsComment,
  setContractsComment,
  onRouteSection,
  onAssign,
  onAccept,
  selectedIntVariant,
  setSelectedIntVariant,
  approverCandidates,
  selectedApproverIds,
  setSelectedApproverIds,
  onSelectIntVariant,
  onCompleteContractsIntStep,
  onUploadStepFile,
  onSubmitApprovers,
  onDecideApproval,
  onSendToSecretariat,
  selectedDomVariant,
  setSelectedDomVariant,
  onSelectDomVariant,
  onCompleteContractsDomStep,
  onScheduleDomStep,
  onUploadDomStepFile,
  onSubmitDomApprovers,
  onDecideDomApproval,
  onSendToContractsAdmin,
  onReturnDomToMarketing,
  onRollbackDomStep,
}: Props & { section: ContractsProcurementSectionType }) {
  const routed = req.contractsProcurementSection;
  const isThisSection = routed === section;
  const otherSection = routed && routed !== section;

  if (otherSection) {
    return (
      <div className="m-3 rounded-xl border border-border/60 bg-foreground/[0.02] px-4 py-6 text-center">
        <p className="text-sm font-semibold text-foreground/70">
          {t("contractsTabs.routedElsewhereTitle")}
        </p>
        <p className="mt-1 text-xs text-foreground/50">
          {section === "Domestic"
            ? t("contractsTabs.routedToInternational")
            : t("contractsTabs.routedToDomestic")}
        </p>
      </div>
    );
  }

  const showIntSteps =
    section === "International" && isThisSection && Boolean(req.contractsAcceptedAt);

  const showDomSteps =
    section === "Domestic" && isThisSection && Boolean(req.contractsAcceptedAt);

  return (
    <div className="space-y-3 p-2">
      {!showIntSteps && !showDomSteps && (
        <ContractsWorkflowPanel
          req={req}
          locale={locale}
          t={t}
          acting={acting}
          contractsPerms={isLivePhase ? contractsPerms : undefined}
          contractsWorkers={contractsWorkers}
          selectedSection={selectedContractsSection}
          setSelectedSection={setSelectedContractsSection}
          selectedEngineer={selectedEngineer}
          setSelectedEngineer={setSelectedEngineer}
          stepComment={contractsComment}
          setStepComment={setContractsComment}
          onRouteSection={onRouteSection}
          onAssign={onAssign}
          onAccept={onAccept}
          sectionScope={section}
        />
      )}

      {showIntSteps && (
        <InternationalContractsWorkflowPanel
          req={req}
          locale={locale}
          t={t}
          acting={acting}
          contractsPerms={isLivePhase ? contractsPerms : undefined}
          stepComment={contractsComment}
          setStepComment={setContractsComment}
          selectedVariant={selectedIntVariant}
          setSelectedVariant={setSelectedIntVariant}
          approverCandidates={approverCandidates}
          selectedApproverIds={selectedApproverIds}
          setSelectedApproverIds={setSelectedApproverIds}
          onSelectVariant={onSelectIntVariant}
          onCompleteStep={onCompleteContractsIntStep}
          onUploadStepFile={onUploadStepFile}
          onSubmitApprovers={onSubmitApprovers}
          onDecideApproval={onDecideApproval}
          onSendToSecretariat={onSendToSecretariat}
        />
      )}

      {showDomSteps && (
        <DomesticContractsWorkflowPanel
          req={req}
          locale={locale}
          t={t}
          acting={acting}
          contractsPerms={isLivePhase ? contractsPerms : undefined}
          stepComment={contractsComment}
          setStepComment={setContractsComment}
          selectedVariant={selectedDomVariant}
          setSelectedVariant={setSelectedDomVariant}
          approverCandidates={approverCandidates}
          selectedApproverIds={selectedApproverIds}
          setSelectedApproverIds={setSelectedApproverIds}
          onSelectVariant={onSelectDomVariant}
          onCompleteStep={onCompleteContractsDomStep}
          onScheduleStep={onScheduleDomStep}
          onUploadStepFile={onUploadDomStepFile}
          onSubmitApprovers={onSubmitDomApprovers}
          onDecideApproval={onDecideDomApproval}
          onSendToContractsAdmin={onSendToContractsAdmin}
          onReturnToMarketing={onReturnDomToMarketing}
          onRollbackStep={onRollbackDomStep}
        />
      )}
    </div>
  );
}

function PaymentTabBody({ t }: { t: ReturnType<typeof useTranslations> }) {
  return (
    <div className="m-3 rounded-xl border border-border/60 bg-foreground/[0.02] px-5 py-8 text-center">
      <div className="mx-auto mb-3 flex h-12 w-12 items-center justify-center rounded-2xl bg-emerald-500/10 text-emerald-600">
        <CreditCard size={22} />
      </div>
      <h3 className="text-sm font-bold text-foreground">{t("contractsTabs.paymentTitle")}</h3>
      <p className="mx-auto mt-2 max-w-md text-xs leading-relaxed text-foreground/55">
        {t("contractsTabs.paymentHint")}
      </p>
      <span className="mt-4 inline-flex rounded-full bg-foreground/[0.06] px-3 py-1 text-[11px] font-bold uppercase tracking-wider text-foreground/45">
        {t("comingSoon")}
      </span>
    </div>
  );
}
