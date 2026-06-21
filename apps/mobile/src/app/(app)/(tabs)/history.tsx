import { ScrollView, Text, View, Pressable } from "react-native";
import { useRouter } from "expo-router";
import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Badge, Card, Spinner } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage, formatDate } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function HistoryScreen() {
  const router = useRouter();
  const t = useT();
  const history = useQuery({ queryKey: ["room-history"], queryFn: roomsApi.history });

  return (
    <ScrollView style={{ flex: 1, backgroundColor: colors.bg }} contentContainerStyle={{ padding: spacing.lg, gap: spacing.sm }}>
      {history.isLoading ? (
        <Spinner />
      ) : history.isError ? (
        <Alert>{errorMessage(history.error)}</Alert>
      ) : history.data && history.data.length > 0 ? (
        history.data.map((item) => (
          <Pressable
            key={`${item.roomId}-${item.joinedAtUtc}`}
            onPress={() => item.status === "Active" && router.push(`/rooms/${item.roomId}`)}
          >
            <Card style={{ flexDirection: "row", justifyContent: "space-between", alignItems: "center" }}>
              <View style={{ flex: 1 }}>
                <Text style={{ color: colors.white, fontWeight: "600" }}>{item.name}</Text>
                <Text style={{ color: colors.textFaint, fontSize: 12 }}>
                  {item.code} · {formatDate(item.joinedAtUtc)}
                </Text>
              </View>
              <View style={{ flexDirection: "row", gap: 6 }}>
                <Badge>{t(item.role === "Host" ? "enums.roleHost" : "enums.roleMember")}</Badge>
                <Badge
                  color={item.status === "Active" ? "#052e1b" : colors.bgElevated}
                  textColor={item.status === "Active" ? colors.success : colors.textFaint}
                >
                  {t(item.status === "Active" ? "enums.statusActive" : "enums.statusClosed")}
                </Badge>
              </View>
            </Card>
          </Pressable>
        ))
      ) : (
        <Text style={{ color: colors.textFaint, fontSize: 13 }}>{t("home.noHistory")}</Text>
      )}
    </ScrollView>
  );
}
