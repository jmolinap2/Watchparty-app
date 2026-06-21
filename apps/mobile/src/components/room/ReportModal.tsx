import { useState } from "react";
import { Modal, Text, View } from "react-native";
import { Alert, Button, Card, TextField } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

interface Props {
  visible: boolean;
  title: string;
  subtitle?: string;
  onClose: () => void;
  onSubmit: (reason: string) => Promise<void>;
}

export function ReportModal({ visible, title, subtitle, onClose, onSubmit }: Props) {
  const t = useT();
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function submit() {
    if (!reason.trim()) return;
    setError(null);
    setLoading(true);
    try {
      await onSubmit(reason.trim());
      setReason("");
      onClose();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <Modal visible={visible} transparent animationType="fade" onRequestClose={onClose}>
      <View style={{ flex: 1, backgroundColor: "rgba(0,0,0,0.6)", justifyContent: "center", padding: spacing.lg }}>
        <Card style={{ gap: spacing.md }}>
          <Text style={{ color: colors.white, fontSize: 17, fontWeight: "700" }}>{title}</Text>
          {subtitle ? <Text style={{ color: colors.textMuted, fontSize: 13 }}>{subtitle}</Text> : null}
          {error ? <Alert>{error}</Alert> : null}
          <TextField
            value={reason}
            onChangeText={setReason}
            placeholder={t("reports.reasonPlaceholder")}
            multiline
            numberOfLines={4}
            style={{ minHeight: 90, textAlignVertical: "top" }}
            maxLength={1000}
          />
          <View style={{ flexDirection: "row", justifyContent: "flex-end", gap: spacing.sm }}>
            <Button title={t("reports.cancel")} variant="ghost" onPress={onClose} />
            <Button title={loading ? t("reports.submitting") : t("reports.submit")} variant="danger" onPress={submit} loading={loading} />
          </View>
        </Card>
      </View>
    </Modal>
  );
}
