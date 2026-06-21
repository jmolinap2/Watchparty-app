import { ScrollView, Text, View } from "react-native";
import { useRouter } from "expo-router";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { usersApi } from "@/lib/api/users";
import { useAuth } from "@/lib/hooks/use-auth";
import { Alert, Avatar, Badge, Button, Card, Spinner } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { useT } from "@/lib/i18n";

export default function ProfileScreen() {
  const router = useRouter();
  const t = useT();
  const { user } = useAuth();
  const queryClient = useQueryClient();
  const blocked = useQuery({ queryKey: ["blocked-users"], queryFn: usersApi.blocked });

  async function unblock(userId: string) {
    await usersApi.unblock(userId);
    queryClient.invalidateQueries({ queryKey: ["blocked-users"] });
  }

  if (!user) return <Spinner />;

  return (
    <ScrollView style={{ flex: 1, backgroundColor: colors.bg }} contentContainerStyle={{ padding: spacing.lg, gap: spacing.lg }}>
      <Card style={{ gap: spacing.md }}>
        <View style={{ flexDirection: "row", alignItems: "center", gap: spacing.md }}>
          <Avatar name={user.displayName} url={user.avatarUrl} size={56} />
          <View style={{ flex: 1 }}>
            <Text style={{ color: colors.white, fontSize: 18, fontWeight: "700" }}>{user.displayName}</Text>
            <Text style={{ color: colors.textFaint, fontSize: 13 }}>{user.email}</Text>
            <View style={{ flexDirection: "row", gap: 6, marginTop: 4 }}>
              <Badge>{t(user.role === "Admin" ? "enums.roleHost" : "enums.roleMember")}</Badge>
              {user.isPrivate ? <Badge>{t("profile.private")}</Badge> : null}
            </View>
          </View>
        </View>
        <Button title={t("profile.editProfile")} variant="secondary" onPress={() => router.push("/edit-profile")} />
      </Card>

      <Card style={{ gap: spacing.sm }}>
        <Text style={{ color: colors.white, fontSize: 16, fontWeight: "700" }}>{t("profile.blockedUsers")}</Text>
        {blocked.isLoading ? (
          <Spinner />
        ) : blocked.data && blocked.data.length > 0 ? (
          blocked.data.map((b) => (
            <View key={b.id} style={{ flexDirection: "row", alignItems: "center", justifyContent: "space-between" }}>
              <View style={{ flexDirection: "row", alignItems: "center", gap: spacing.sm }}>
                <Avatar name={b.displayName} url={b.avatarUrl} size={28} />
                <Text style={{ color: colors.text }}>{b.displayName}</Text>
              </View>
              <Button title={t("profile.unblock")} variant="ghost" onPress={() => unblock(b.id)} />
            </View>
          ))
        ) : (
          <Text style={{ color: colors.textFaint, fontSize: 13 }}>{t("profile.noBlocked")}</Text>
        )}
      </Card>
    </ScrollView>
  );
}
