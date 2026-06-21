import { useState } from "react";
import { ScrollView } from "react-native";
import { useRouter } from "expo-router";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Button, Card, Field, TextField } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function JoinRoomScreen() {
  const router = useRouter();
  const t = useT();
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    setLoading(true);
    try {
      const detail = await roomsApi.join(code.trim());
      router.replace(`/rooms/${detail.room.id}`);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView style={{ flex: 1, backgroundColor: colors.bg }} contentContainerStyle={{ padding: spacing.lg }}>
      <Card style={{ gap: spacing.md }}>
        {error ? <Alert>{error}</Alert> : null}
        <Field label={t("room.join.codeLabel")}>
          <TextField
            value={code}
            onChangeText={(v) => setCode(v.toUpperCase())}
            autoCapitalize="characters"
            maxLength={6}
            placeholder={t("room.join.codePlaceholder")}
          />
        </Field>
        <Button title={loading ? t("room.join.joining") : t("room.join.joinRoom")} onPress={onSubmit} loading={loading} />
      </Card>
    </ScrollView>
  );
}
