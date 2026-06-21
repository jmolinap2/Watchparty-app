import { useEffect, useState } from "react";
import { ScrollView, Switch, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { usersApi } from "@/lib/api/users";
import { useAuth } from "@/lib/hooks/use-auth";
import { Alert, Button, Card, Field, TextField } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function EditProfileScreen() {
  const router = useRouter();
  const t = useT();
  const { user, setUser } = useAuth();

  const [displayName, setDisplayName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [avatarUrl, setAvatarUrl] = useState("");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (user) {
      setDisplayName(user.displayName);
      setIsPrivate(user.isPrivate);
      setAvatarUrl(user.avatarUrl ?? "");
    }
  }, [user]);

  async function saveProfile() {
    setError(null);
    setMessage(null);
    setLoading(true);
    try {
      await usersApi.updateProfile({ displayName: displayName.trim(), isPrivate });
      const updated = await usersApi.setAvatar(avatarUrl.trim() || null);
      setUser(updated);
      if (currentPassword && newPassword) {
        await usersApi.changePassword({ currentPassword, newPassword });
        setCurrentPassword("");
        setNewPassword("");
      }
      setMessage(t("editProfile.saved"));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView style={{ flex: 1, backgroundColor: colors.bg }} contentContainerStyle={{ padding: spacing.lg, gap: spacing.lg }}>
      <Card style={{ gap: spacing.md }}>
        {error ? <Alert>{error}</Alert> : null}
        {message ? <Alert kind="success">{message}</Alert> : null}
        <Field label={t("editProfile.displayName")}>
          <TextField value={displayName} onChangeText={setDisplayName} maxLength={40} />
        </Field>
        <Field label={t("editProfile.avatarUrl")} hint={t("editProfile.avatarHint")}>
          <TextField value={avatarUrl} onChangeText={setAvatarUrl} autoCapitalize="none" placeholder="https://…" />
        </Field>
        <View style={{ flexDirection: "row", alignItems: "center", justifyContent: "space-between" }}>
          <Text style={{ color: colors.text }}>{t("editProfile.privateProfile")}</Text>
          <Switch value={isPrivate} onValueChange={setIsPrivate} />
        </View>
      </Card>

      <Card style={{ gap: spacing.md }}>
        <Text style={{ color: colors.white, fontWeight: "700" }}>{t("editProfile.changePasswordOptional")}</Text>
        <Field label={t("editProfile.currentPassword")}>
          <TextField value={currentPassword} onChangeText={setCurrentPassword} secureTextEntry />
        </Field>
        <Field label={t("editProfile.newPassword")} hint={t("auth.passwordHint")}>
          <TextField value={newPassword} onChangeText={setNewPassword} secureTextEntry />
        </Field>
      </Card>

      <Button title={loading ? t("editProfile.saving") : t("editProfile.save")} onPress={saveProfile} loading={loading} />
    </ScrollView>
  );
}
