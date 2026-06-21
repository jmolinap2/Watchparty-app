import { apiRequest } from "@/lib/api/http";
import type { ChatMessageDto } from "@/lib/api/types";

export const chatApi = {
  history: (roomId: string, opts: { before?: string; limit?: number } = {}) =>
    apiRequest<ChatMessageDto[]>(`/api/rooms/${roomId}/chat/messages`, {
      query: { before: opts.before, limit: opts.limit ?? 50 },
    }),

  send: (roomId: string, content: string) =>
    apiRequest<ChatMessageDto>(`/api/rooms/${roomId}/chat/messages`, {
      method: "POST",
      body: { content },
    }),

  remove: (roomId: string, messageId: string) =>
    apiRequest<void>(`/api/rooms/${roomId}/chat/messages/${messageId}`, { method: "DELETE" }),
};
