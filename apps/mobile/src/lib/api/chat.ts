import { apiRequest } from "@/lib/api/http";
import type { ChatMessageDto } from "@/lib/api/types";

export const chatApi = {
  history: (roomId: string, opts: { before?: string; limit?: number } = {}) =>
    apiRequest<ChatMessageDto[]>(`/api/rooms/${roomId}/chat/messages`, {
      query: { before: opts.before, limit: opts.limit ?? 50 },
    }),
};
