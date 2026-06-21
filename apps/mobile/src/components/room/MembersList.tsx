import { FlatList, Pressable, Text, View } from "react-native";
import { Avatar, Badge } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import type { RoomMemberDto } from "@/lib/api/types";
import { useT } from "@/lib/i18n";

interface Props {
  members: RoomMemberDto[];
  onlineIds: string[];
  meId: string | undefined;
  isHost: boolean;
  onTransferHost: (userId: string) => void;
  onKick: (userId: string) => void;
  onReport: (member: RoomMemberDto) => void;
}

export function MembersList({ members, onlineIds, meId, isHost, onTransferHost, onKick, onReport }: Props) {
  const t = useT();
  const online = new Set(onlineIds);
  const sorted = [...members].sort((a, b) => {
    const ao = online.has(a.userId) ? 0 : 1;
    const bo = online.has(b.userId) ? 0 : 1;
    if (ao !== bo) return ao - bo;
    return a.displayName.localeCompare(b.displayName);
  });

  return (
    <FlatList
      data={sorted}
      keyExtractor={(m) => m.userId}
      contentContainerStyle={{ padding: spacing.md, gap: spacing.sm }}
      renderItem={({ item }) => {
        const isOnline = online.has(item.userId);
        const isMe = item.userId === meId;
        return (
          <View style={{ flexDirection: "row", alignItems: "center", gap: spacing.sm }}>
            <View>
              <Avatar name={item.displayName} url={item.avatarUrl} size={32} />
              <View
                style={{
                  position: "absolute",
                  bottom: -1,
                  right: -1,
                  width: 11,
                  height: 11,
                  borderRadius: 6,
                  borderWidth: 2,
                  borderColor: colors.card,
                  backgroundColor: isOnline ? colors.online : colors.offline,
                }}
              />
            </View>
            <View style={{ flex: 1, flexDirection: "row", alignItems: "center", gap: 6 }}>
              <Text style={{ color: colors.text }}>
                {item.displayName}
                {isMe ? t("members.youSuffix") : ""}
              </Text>
              {item.role === "Host" ? (
                <Badge color={colors.primaryDark} textColor={colors.white}>
                  {t("members.hostBadge")}
                </Badge>
              ) : null}
            </View>
            <View style={{ flexDirection: "row", gap: spacing.md }}>
              {isHost && !isMe ? (
                <>
                  <Pressable onPress={() => onTransferHost(item.userId)}>
                    <Text style={{ color: colors.primary, fontSize: 12 }}>{t("members.makeHost")}</Text>
                  </Pressable>
                  <Pressable onPress={() => onKick(item.userId)}>
                    <Text style={{ color: colors.danger, fontSize: 12 }}>{t("members.kick")}</Text>
                  </Pressable>
                </>
              ) : null}
              {!isMe ? (
                <Pressable onPress={() => onReport(item)}>
                  <Text style={{ color: colors.warning, fontSize: 12 }}>{t("members.report")}</Text>
                </Pressable>
              ) : null}
            </View>
          </View>
        );
      }}
    />
  );
}
