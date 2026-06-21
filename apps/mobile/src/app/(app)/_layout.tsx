import { Stack } from "expo-router";
import { colors } from "@/lib/theme";
import { useT } from "@/lib/i18n";

export default function AppLayout() {
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
      <Stack.Screen name="(tabs)" options={{ headerShown: false }} />
      <Stack.Screen name="rooms/[id]" options={{ title: t("room.screenTitle") }} />
      <Stack.Screen name="rooms/create" options={{ title: t("room.create.submit"), presentation: "modal" }} />
      <Stack.Screen name="rooms/join" options={{ title: t("room.join.joinRoom"), presentation: "modal" }} />
      <Stack.Screen name="edit-profile" options={{ title: t("profile.editProfile"), presentation: "modal" }} />
    </Stack>
  );
}
