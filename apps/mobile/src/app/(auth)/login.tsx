import { useState } from "react";
import { ScrollView, Text, View } from "react-native";
import { Link, useRouter } from "expo-router";
import { useAuth } from "@/lib/hooks/use-auth";
import { Alert, Button, Card, Field, TextField } from "@/components/ui";
import { colors, spacing } from "@/lib/theme";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function LoginScreen() {
  const router = useRouter();
  const t = useT();
  const { login } = useAuth();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit() {
    setError(null);
    setLoading(true);
    try {
      await login({ email: email.trim(), password });
      router.replace("/home");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <ScrollView contentContainerStyle={{ padding: spacing.lg, gap: spacing.lg, flexGrow: 1, justifyContent: "center" }}>
      <Text style={{ color: colors.white, fontSize: 28, fontWeight: "800", textAlign: "center" }}>
        Watch<Text style={{ color: colors.primary }}>Party</Text>
      </Text>
      <Card style={{ gap: spacing.md }}>
        {error ? <Alert>{error}</Alert> : null}
        <Field label={t("auth.email")}>
          <TextField
            value={email}
            onChangeText={setEmail}
            autoCapitalize="none"
            keyboardType="email-address"
            autoComplete="email"
          />
        </Field>
        <Field label={t("auth.password")}>
          <TextField value={password} onChangeText={setPassword} secureTextEntry autoComplete="password" />
        </Field>
        <Button title={loading ? t("auth.signingIn") : t("auth.signIn")} onPress={onSubmit} loading={loading} />
        <View style={{ flexDirection: "row", justifyContent: "space-between" }}>
          <Link href="/forgot-password" style={{ color: colors.primary, fontSize: 13 }}>
            {t("auth.forgotPassword")}
          </Link>
          <Link href="/register" style={{ color: colors.primary, fontSize: 13 }}>
            {t("auth.createAccount")}
          </Link>
        </View>
      </Card>
    </ScrollView>
  );
}
