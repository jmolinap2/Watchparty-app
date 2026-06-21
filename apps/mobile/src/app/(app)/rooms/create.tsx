import { useState } from "react";
import { ScrollView, Switch, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { roomsApi } from "@/lib/api/rooms";
import { Alert, Button, Card, Field, TextField } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function CreateRoomScreen() {
  const router = useRouter();
  const t = useT();
  const [name, setName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [maxMembers, setMaxMembers] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    setLoading(true);
    try {
      const room = await roomsApi.create({
        name: name.trim(),
        isPrivate,
        maxMembers: maxMembers ? Number(maxMembers) : null,
      });
      router.replace(`/rooms/${room.id}`);
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
        <Field label={t("room.create.nameLabel")}>
          <TextField value={name} onChangeText={setName} maxLength={60} />
        </Field>
        <Field label={t("room.create.maxMembers")} hint={t("room.create.maxMembersHint")}>
          <TextField value={maxMembers} onChangeText={setMaxMembers} keyboardType="number-pad" placeholder={t("room.create.maxMembersPlaceholder")} />
        </Field>
        <View style={{ flexDirection: "row", alignItems: "center", justifyContent: "space-between" }}>
          <Text style={{ color: colors.text }}>{t("room.create.privateRoom")}</Text>
          <Switch value={isPrivate} onValueChange={setIsPrivate} />
        </View>
        <Button title={loading ? t("room.create.creating") : t("room.create.submit")} onPress={onSubmit} loading={loading} />
      </Card>
    </ScrollView>
  );
}
