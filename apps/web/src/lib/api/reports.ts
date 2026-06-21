import { apiRequest } from "@/lib/api/http";
import type { ReportDto } from "@/lib/api/types";

export const reportsApi = {
  reportUser: (targetUserId: string, reason: string, roomId: string | null = null) =>
    apiRequest<ReportDto>("/api/reports/users", {
      method: "POST",
      body: { targetUserId, roomId, reason },
    }),

  reportMessage: (messageId: string, reason: string) =>
    apiRequest<ReportDto>("/api/reports/messages", {
      method: "POST",
      body: { messageId, reason },
    }),

  mine: () => apiRequest<ReportDto[]>("/api/reports/mine"),
};
