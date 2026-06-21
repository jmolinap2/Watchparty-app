import { useState } from "react";
import { ScrollView } from "react-native";
import { authApi } from "@/lib/api/auth";
import { Alert, Button, Card, Field, TextField } from "@/components/ui";
import { spacing } from "@/lib/theme";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function ForgotPasswordScreen() {
  const t = useT();
  const [email, setEmail] = useState("");
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    setLoading(true);
    try {
      await authApi.forgotPassword(email.trim());
      setDone(true);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={{ padding: spacing.lg, gap: spacing.lg, flexGrow: 1, justifyContent: "center" }}>
      <Card style={{ gap: spacing.md }}>
        {done ? (
          <Alert kind="success">{t("auth.resetSent")}</Alert>
        ) : (
          <>
            {error ? <Alert>{error}</Alert> : null}
            <Field label={t("auth.email")} hint={t("auth.forgotHint")}>
              <TextField
                value={email}
                onChangeText={setEmail}
                autoCapitalize="none"
                keyboardType="email-address"
              />
            </Field>
            <Button title={loading ? t("auth.sending") : t("auth.sendResetLink")} onPress={onSubmit} loading={loading} />
          </>
        )}
      </Card>
    </ScrollView>
  );
}
