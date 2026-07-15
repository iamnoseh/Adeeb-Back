import { useTranslation } from "react-i18next";

export function useMmtLabels() {
  const { t } = useTranslation();
  return {
    admissionTypes: [t("mmt.enum.budget"), t("mmt.enum.contract")],
    studyForms: [
      t("mmt.enum.fullTime"),
      t("mmt.enum.partTime"),
      t("mmt.enum.distance"),
      t("mmt.enum.other"),
    ],
    studyLanguages: [
      t("mmt.enum.tajik"),
      t("mmt.enum.russian"),
      t("mmt.enum.english"),
      t("mmt.enum.other"),
    ],
    distributionRounds: [
      t("mmt.enum.mainDistribution"),
      t("mmt.enum.repeatDistribution"),
      t("mmt.enum.additionalDistribution"),
      t("mmt.enum.other"),
    ],
    universityTypes: [
      t("mmt.enum.public"),
      t("mmt.enum.private"),
      t("mmt.enum.other"),
    ],
    scoreModes: [
      t("mmt.enum.skipExisting"),
      t("mmt.enum.updateExisting"),
      t("mmt.enum.failExisting"),
    ],
    unknown: t("mmt.enum.unknown"),
  };
}
