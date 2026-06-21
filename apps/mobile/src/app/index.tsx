import { View } from "react-native";
import { Spinner } from "@/components/ui";
import { colors } from "@/lib/theme";

// The root layout's auth gate redirects to /login or /home once the session has
// rehydrated; this screen just shows a spinner in the meantime.
export default function Index() {
  return (
    <View style={{ flex: 1, alignItems: "center", justifyContent: "center", backgroundColor: colors.bg }}>
      <Spinner />
    </View>
  );
}
