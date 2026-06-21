import { Stack } from "expo-router";
import { colors } from "@/lib/theme";
import { useT } from "@/lib/i18n";

export default function AuthLayout() {
  const t = useT();
  return (
    <Stack
      screenOptions={{
        headerStyle: { backgroundColor: colors.bg },
        headerTintColor: colors.text,
        headerShadowVisible: false,
        contentStyle: { backgroundColor: colors.bg },
      }}
    >
      <Stack.Screen name="login" options={{ title: t("auth.signIn") }} />
      <Stack.Screen name="register" options={{ title: t("auth.createAccount") }} />
      <Stack.Screen name="forgot-password" options={{ title: t("auth.resetTitle") }} />
    </Stack>
  );
}
