import { type ReactNode } from "react";
import {
  ActivityIndicator,
  Image,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  type TextInputProps,
  View,
  type ViewStyle,
} from "react-native";
import { colors, radius, spacing } from "@/lib/theme";
import { initials } from "@/lib/utils";

type ButtonVariant = "primary" | "secondary" | "ghost" | "danger";

export function Button({
  title,
  onPress,
  variant = "primary",
  disabled,
  loading,
  style,
}: {
  title: string;
  onPress: () => void;
  variant?: ButtonVariant;
  disabled?: boolean;
  loading?: boolean;
  style?: ViewStyle;
}) {
  const bg = {
    primary: colors.primary,
    secondary: colors.bgElevated,
    ghost: "transparent",
    danger: colors.danger,
  }[variant];
  const fg = variant === "secondary" ? colors.text : colors.white;

  return (
    <Pressable
      onPress={onPress}
      disabled={disabled || loading}
      style={({ pressed }) => [
        styles.button,
        { backgroundColor: bg, opacity: disabled || loading ? 0.5 : pressed ? 0.85 : 1 },
        style,
      ]}
    >
      {loading ? (
        <ActivityIndicator color={fg} />
      ) : (
        <Text style={[styles.buttonText, { color: variant === "ghost" ? colors.textMuted : fg }]}>
          {title}
        </Text>
      )}
    </Pressable>
  );
}

export function TextField({ style, ...props }: TextInputProps) {
  return (
    <TextInput
      placeholderTextColor={colors.textFaint}
      style={[styles.input, style]}
      {...props}
    />
  );
}

export function Field({ label, children, hint }: { label: string; children: ReactNode; hint?: string }) {
  return (
    <View style={{ gap: spacing.xs }}>
      <Text style={styles.label}>{label}</Text>
      {children}
      {hint ? <Text style={styles.hint}>{hint}</Text> : null}
    </View>
  );
}

export function Card({ children, style }: { children: ReactNode; style?: ViewStyle }) {
  return <View style={[styles.card, style]}>{children}</View>;
}

export function Screen({ children, style }: { children: ReactNode; style?: ViewStyle }) {
  return <View style={[styles.screen, style]}>{children}</View>;
}

export function Spinner() {
  return (
    <View style={{ padding: spacing.lg, alignItems: "center" }}>
      <ActivityIndicator color={colors.primary} />
    </View>
  );
}

export function Alert({ children, kind = "error" }: { children: ReactNode; kind?: "error" | "success" | "info" }) {
  const palette = {
    error: { bg: "#4c0519", fg: "#fecdd3" },
    success: { bg: "#052e1b", fg: "#a7f3d0" },
    info: { bg: "#082f49", fg: "#bae6fd" },
  }[kind];
  return (
    <View style={[styles.alert, { backgroundColor: palette.bg }]}>
      <Text style={{ color: palette.fg, fontSize: 13 }}>{children}</Text>
    </View>
  );
}

export function Badge({ children, color = colors.bgElevated, textColor = colors.textMuted }: {
  children: ReactNode;
  color?: string;
  textColor?: string;
}) {
  return (
    <View style={[styles.badge, { backgroundColor: color }]}>
      <Text style={{ color: textColor, fontSize: 11, fontWeight: "600" }}>{children}</Text>
    </View>
  );
}

export function Avatar({ name, url, size = 36 }: { name: string; url?: string | null; size?: number }) {
  if (url) {
    return <Image source={{ uri: url }} style={{ width: size, height: size, borderRadius: size / 2 }} />;
  }
  return (
    <View
      style={{
        width: size,
        height: size,
        borderRadius: size / 2,
        backgroundColor: colors.primaryDark,
        alignItems: "center",
        justifyContent: "center",
      }}
    >
      <Text style={{ color: colors.white, fontWeight: "700", fontSize: size * 0.38 }}>
        {initials(name)}
      </Text>
    </View>
  );
}

export const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: colors.bg, padding: spacing.lg },
  button: {
    borderRadius: radius.md,
    paddingVertical: 12,
    paddingHorizontal: spacing.lg,
    alignItems: "center",
    justifyContent: "center",
  },
  buttonText: { fontSize: 15, fontWeight: "600" },
  input: {
    borderWidth: 1,
    borderColor: colors.border,
    backgroundColor: colors.bgElevated,
    borderRadius: radius.md,
    paddingHorizontal: spacing.md,
    paddingVertical: 10,
    color: colors.text,
    fontSize: 15,
  },
  label: { color: colors.textMuted, fontSize: 13, fontWeight: "600" },
  hint: { color: colors.textFaint, fontSize: 12 },
  card: {
    backgroundColor: colors.card,
    borderRadius: radius.lg,
    borderWidth: 1,
    borderColor: colors.border,
    padding: spacing.lg,
  },
  alert: { borderRadius: radius.md, paddingHorizontal: spacing.md, paddingVertical: 10 },
  badge: { borderRadius: radius.full, paddingHorizontal: 8, paddingVertical: 2 },
});
