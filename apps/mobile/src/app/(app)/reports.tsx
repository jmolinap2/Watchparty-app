import { Stack } from "expo-router";
import { ScrollView, Text, View } from "react-native";
import { useQuery } from "@tanstack/react-query";
import { reportsApi } from "@/lib/api/reports";
import { Alert, Badge, Card, Spinner } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage, formatDate } from "@/lib/utils";
import { useT, type MessageKey } from "@/lib/i18n";

const statusColors: Record<string, { bg: string; fg: string }> = {
  Pending: { bg: "#3b2f0a", fg: colors.warning },
  Resolved: { bg: "#052e1b", fg: colors.success },
  Rejected: { bg: colors.bgElevated, fg: colors.textFaint },
};

const statusKey: Record<string, MessageKey> = {
  Pending: "enums.reportPending",
  Resolved: "enums.reportResolved",
  Rejected: "enums.reportRejected",
};

export default function ReportsScreen() {
  const t = useT();
  const reports = useQuery({ queryKey: ["my-reports"], queryFn: reportsApi.mine });

  return (
    <>
      <Stack.Screen options={{ title: t("reports.screenTitle") }} />
      <ScrollView style={{ flex: 1, backgroundColor: colors.bg }} contentContainerStyle={{ padding: spacing.lg, gap: spacing.sm }}>
        {reports.isLoading ? (
          <Spinner />
        ) : reports.isError ? (
          <Alert>{errorMessage(reports.error)}</Alert>
        ) : reports.data && reports.data.length > 0 ? (
          reports.data.map((r) => {
            const sc = statusColors[r.status] ?? { bg: colors.bgElevated, fg: colors.textFaint };
            return (
              <Card key={r.id} style={{ gap: 6 }}>
                <View style={{ flexDirection: "row", gap: 6 }}>
                  <Badge>{t(r.type === "Message" ? "reports.typeMessage" : "reports.typeUser")}</Badge>
                  <Badge color={sc.bg} textColor={sc.fg}>
                    {statusKey[r.status] ? t(statusKey[r.status]) : r.status}
                  </Badge>
                </View>
                <Text style={{ color: colors.text, fontSize: 14 }}>{r.reason}</Text>
                {r.resolutionNote ? (
                  <Text style={{ color: colors.textFaint, fontSize: 12 }}>
                    {t("reports.note", { note: r.resolutionNote })}
                  </Text>
                ) : null}
                <Text style={{ color: colors.textFaint, fontSize: 11 }}>{formatDate(r.createdAtUtc)}</Text>
              </Card>
            );
          })
        ) : (
          <Text style={{ color: colors.textFaint, fontSize: 13 }}>{t("reports.empty")}</Text>
        )}
      </ScrollView>
    </>
  );
}
