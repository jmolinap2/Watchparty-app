import { useState } from "react";
import { ScrollView } from "react-native";
import { useRouter } from "expo-router";
import { useAuth } from "@/lib/hooks/use-auth";
import { Alert, Button, Card, Field, TextField } from "@/components/ui";
import { spacing } from "@/lib/theme";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function RegisterScreen() {
  const router = useRouter();
  const t = useT();
  const { register } = useAuth();
  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    setLoading(true);
    try {
      await register({ email: email.trim(), password, displayName: displayName.trim() });
      router.replace("/home");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={{ padding: spacing.lg, gap: spacing.lg, flexGrow: 1, justifyContent: "center" }}>
      <Card style={{ gap: spacing.md }}>
        {error ? <Alert>{error}</Alert> : null}
        <Field label={t("auth.displayName")}>
          <TextField value={displayName} onChangeText={setDisplayName} maxLength={40} />
        </Field>
        <Field label={t("auth.email")}>
          <TextField
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            keyboardType="email-address"
          />
        </Field>
        <Field label={t("auth.password")} hint={t("auth.passwordHint")}>
          <TextField value={password} onChangeText={setPassword} secureTextEntry />
        </Field>
        <Button title={loading ? t("auth.creating") : t("auth.createAccount")} onPress={onSubmit} loading={loading} />
      </Card>
    </ScrollView>
  );
}
