import { useRef, useState } from "react";
import { FlatList, Pressable, Text, View } from "react-native";
import { Avatar, Button, TextField } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import type { ChatMessageDto } from "@/lib/api/types";
import { relativeTime } from "@/lib/utils";
import { useT } from "@/lib/i18n";

interface Props {
  messages: ChatMessageDto[];
  meId: string | undefined;
  canModerate: boolean;
  onSend: (content: string) => void;
  onDelete: (messageId: string) => void;
  onReport: (message: ChatMessageDto) => void;
}

export function ChatList({ messages, meId, canModerate, onSend, onDelete, onReport }: Props) {
  const t = useT();
  const [text, setText] = useState("");
  const listRef = useRef<FlatList<ChatMessageDto>>(null);

  function submit() {
    const content = text.trim();
    if (!content) return;
    onSend(content);
    setText("");
  }

  return (
    <View style={{ flex: 1 }}>
      <FlatList
        ref={listRef}
        data={messages}
        keyExtractor={(m) => m.id}
        contentContainerStyle={{ padding: spacing.md, gap: spacing.md }}
        onContentSizeChange={() => listRef.current?.scrollToEnd({ animated: true })}
        ListEmptyComponent={
          <Text style={{ color: colors.textFaint, fontSize: 13 }}>{t("chat.empty")}</Text>
        }
        renderItem={({ item }) => (
          <View style={{ flexDirection: "row", gap: spacing.sm }}>
            <Avatar name={item.senderDisplayName} url={item.senderAvatarUrl} size={28} />
            <View style={{ flex: 1 }}>
              <View style={{ flexDirection: "row", alignItems: "center", gap: 6 }}>
                <Text style={{ color: colors.text, fontWeight: "600", fontSize: 13 }}>
                  {item.senderDisplayName}
                </Text>
                <Text style={{ color: colors.textFaint, fontSize: 10 }}>
                  {relativeTime(item.createdAtUtc, t)}
                </Text>
              </View>
              {item.isDeleted ? (
                <Text style={{ color: colors.textFaint, fontStyle: "italic", fontSize: 13 }}>
                  {t("chat.deleted")}
                </Text>
              ) : (
                <Text style={{ color: colors.textMuted, fontSize: 14 }}>{item.content}</Text>
              )}
              {!item.isDeleted ? (
                <View style={{ flexDirection: "row", gap: spacing.md, marginTop: 2 }}>
                  {(item.senderUserId === meId || canModerate) && (
                    <Pressable onPress={() => onDelete(item.id)}>
                      <Text style={{ color: colors.textFaint, fontSize: 11 }}>{t("chat.deleteAction")}</Text>
                    </Pressable>
                  )}
                  {item.senderUserId !== meId && (
                    <Pressable onPress={() => onReport(item)}>
                      <Text style={{ color: colors.warning, fontSize: 11 }}>{t("chat.reportAction")}</Text>
                    </Pressable>
                  )}
                </View>
              ) : null}
            </View>
          </View>
        )}
      />
      <View
        style={{
          flexDirection: "row",
          gap: spacing.sm,
          padding: spacing.sm,
          borderTopWidth: 1,
          borderTopColor: colors.border,
        }}
      >
        <TextField
          style={{ flex: 1 }}
          value={text}
          onChangeText={setText}
          placeholder={t("chat.placeholder")}
          maxLength={1000}
          onSubmitEditing={submit}
        />
        <Button title={t("chat.send")} onPress={submit} />
      </View>
    </View>
  );
}
