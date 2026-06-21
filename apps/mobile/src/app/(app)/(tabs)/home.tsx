import { useState } from "react";
import { ScrollView, Text, View, Pressable } from "react-native";
import { useRouter } from "expo-router";
import { useQuery } from "@tanstack/react-query";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Badge, Button, Card, Field, TextField, Spinner } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage, relativeTime } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function HomeScreen() {
  const router = useRouter();
  const t = useT();
  const [code, setCode] = useState("");
  const [joinError, setJoinError] = useState<string | null>(null);
  const [joining, setJoining] = useState(false);

  const history = useQuery({ queryKey: ["room-history"], queryFn: roomsApi.history });

  async function quickJoin() {
    if (!code.trim()) return;
    setJoinError(null);
    setJoining(true);
    try {
      const detail = await roomsApi.join(code.trim());
      router.push(`/rooms/${detail.room.id}`);
    } catch (err) {
      setJoinError(errorMessage(err));
    } finally {
      setJoining(false);
    }
  }

  return (
    <ScrollView style={{ flex: 1, backgroundColor: colors.bg }} contentContainerStyle={{ padding: spacing.lg, gap: spacing.lg }}>
      <Card style={{ gap: spacing.sm }}>
        <Text style={{ color: colors.white, fontSize: 17, fontWeight: "700" }}>{t("home.createTitle")}</Text>
        <Text style={{ color: colors.textMuted, fontSize: 13 }}>{t("home.createDesc")}</Text>
        <Button title={t("home.createRoom")} onPress={() => router.push("/rooms/create")} />
      </Card>

      <Card style={{ gap: spacing.sm }}>
        <Text style={{ color: colors.white, fontSize: 17, fontWeight: "700" }}>{t("home.joinTitle")}</Text>
        {joinError ? <Alert>{joinError}</Alert> : null}
        <Field label={t("room.join.codeLabel")}>
          <TextField
            value={code}
            onChangeText={(v) => setCode(v.toUpperCase())}
            autoCapitalize="characters"
            maxLength={6}
            placeholder={t("room.join.codePlaceholder")}
          />
        </Field>
        <Button title={joining ? t("room.join.joining") : t("room.join.join")} onPress={quickJoin} loading={joining} />
      </Card>

      <View style={{ gap: spacing.sm }}>
        <Text style={{ color: colors.white, fontSize: 17, fontWeight: "700" }}>{t("home.recentRooms")}</Text>
        {history.isLoading ? (
          <Spinner />
        ) : history.isError ? (
          <Alert>{errorMessage(history.error)}</Alert>
        ) : history.data && history.data.length > 0 ? (
          history.data.slice(0, 8).map((item) => (
            <Pressable
              key={`${item.roomId}-${item.joinedAtUtc}`}
              onPress={() => item.status === "Active" && router.push(`/rooms/${item.roomId}`)}
            >
              <Card style={{ flexDirection: "row", justifyContent: "space-between", alignItems: "center" }}>
                <View>
                  <Text style={{ color: colors.white, fontWeight: "600" }}>{item.name}</Text>
                  <Text style={{ color: colors.textFaint, fontSize: 12 }}>
                    {item.code} · {relativeTime(item.joinedAtUtc, t)}
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
          <Text style={{ color: colors.textFaint, fontSize: 13 }}>{t("home.noRoomsCta")}</Text>
        )}
      </View>
    </ScrollView>
  );
}
