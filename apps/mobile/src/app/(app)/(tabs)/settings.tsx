import { ScrollView, Text, View, Pressable } from "react-native";
import { useRouter } from "expo-router";
import { useAuth } from "@/lib/hooks/use-auth";
import { Button, Card } from "@/components/ui";
import { API_URL } from "@/lib/env";
import { colors, spacing } from "@/lib/theme";
import { useI18n, useT } from "@/lib/i18n";
import { locales } from "@/lib/i18n/config";

export default function SettingsScreen() {
  const router = useRouter();
  const t = useT();
  const { locale, setLocale } = useI18n();
  const { logout } = useAuth();

  return (
    <ScrollView style={{ flex: 1, backgroundColor: colors.bg }} contentContainerStyle={{ padding: spacing.lg, gap: spacing.lg }}>
      <Card style={{ gap: spacing.sm }}>
        <Text style={{ color: colors.white, fontSize: 16, fontWeight: "700" }}>{t("settings.language")}</Text>
        <View style={{ flexDirection: "row", gap: spacing.sm }}>
          {locales.map((l) => (
            <Pressable
              key={l}
              onPress={() => setLocale(l)}
              style={{
                paddingVertical: 8,
                paddingHorizontal: 16,
                borderRadius: 8,
                backgroundColor: locale === l ? colors.primary : colors.bgElevated,
              }}
            >
              <Text style={{ color: locale === l ? colors.white : colors.textMuted, fontWeight: "600" }}>
                {l === "es" ? t("settings.languageEs") : t("settings.languageEn")}
              </Text>
            </Pressable>
          ))}
        </View>
      </Card>

      <Card style={{ gap: spacing.sm }}>
        <Text style={{ color: colors.white, fontSize: 16, fontWeight: "700" }}>{t("settings.moderation")}</Text>
        <Pressable onPress={() => router.push("/reports")}>
          <Text style={{ color: colors.primary, fontSize: 14, paddingVertical: 6 }}>{t("settings.myReports")}</Text>
        </Pressable>
      </Card>

      <Card style={{ gap: spacing.xs }}>
        <Text style={{ color: colors.white, fontSize: 16, fontWeight: "700" }}>{t("settings.about")}</Text>
        <Text style={{ color: colors.textMuted, fontSize: 13 }}>{t("settings.aboutApp")}</Text>
        <Text style={{ color: colors.textFaint, fontSize: 12 }}>{t("settings.apiLabel", { url: API_URL })}</Text>
      </Card>

      <Button title={t("settings.signOut")} variant="danger" onPress={() => logout().then(() => router.replace("/login"))} />
    </ScrollView>
  );
}
