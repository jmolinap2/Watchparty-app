"use client";

import { useEffect, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { usersApi } from "@/lib/api/users";
import { useAuth } from "@/lib/hooks/use-auth";
import { Alert, Avatar, Button, Card, Field, Input, Spinner } from "@/components/ui";
import { errorMessage } from "@/lib/utils";
import { useT } from "@/lib/i18n";

export default function ProfilePage() {
  const t = useT();
  const { user, setUser } = useAuth();
  const queryClient = useQueryClient();

  const [displayName, setDisplayName] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [avatarUrl, setAvatarUrl] = useState("");
  const [profileMsg, setProfileMsg] = useState<string | null>(null);
  const [profileErr, setProfileErr] = useState<string | null>(null);
  const [savingProfile, setSavingProfile] = useState(false);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [pwMsg, setPwMsg] = useState<string | null>(null);
  const [pwErr, setPwErr] = useState<string | null>(null);
  const [savingPw, setSavingPw] = useState(false);

  useEffect(() => {
    if (user) {
      setDisplayName(user.displayName);
      setIsPrivate(user.isPrivate);
      setAvatarUrl(user.avatarUrl ?? "");
    }
  }, [user]);

  const blocked = useQuery({ queryKey: ["blocked-users"], queryFn: usersApi.blocked });

  async function saveProfile(e: React.FormEvent) {
    e.preventDefault();
    setProfileErr(null);
    setProfileMsg(null);
    setSavingProfile(true);
    try {
      const updated = await usersApi.updateProfile({ displayName: displayName.trim(), isPrivate });
      const withAvatar =
        (avatarUrl.trim() || null) !== (updated.avatarUrl ?? null)
          ? await usersApi.setAvatar(avatarUrl.trim() || null)
          : updated;
      setUser(withAvatar);
      setProfileMsg(t("profile.saved"));
    } catch (err) {
      setProfileErr(errorMessage(err));
    } finally {
      setSavingProfile(false);
    }
  }

  async function changePassword(e: React.FormEvent) {
    e.preventDefault();
    setPwErr(null);
    setPwMsg(null);
    setSavingPw(true);
    try {
      await usersApi.changePassword({ currentPassword, newPassword });
      setPwMsg(t("profile.passwordChanged"));
      setCurrentPassword("");
      setNewPassword("");
    } catch (err) {
      setPwErr(errorMessage(err));
    } finally {
      setSavingPw(false);
    }
  }

  async function unblock(userId: string) {
    await usersApi.unblock(userId);
    queryClient.invalidateQueries({ queryKey: ["blocked-users"] });
  }

  if (!user) return <Spinner />;

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <h1 className="text-2xl font-bold text-white">{t("profile.title")}</h1>

      <Card>
        <div className="mb-4 flex items-center gap-3">
          <Avatar name={user.displayName} url={avatarUrl || user.avatarUrl} size={56} />
          <div>
            <div className="font-medium text-white">{user.email}</div>
            <div className="text-xs text-slate-500">
              {t(user.role === "Admin" ? "enums.roleHost" : "enums.roleMember")} ·{" "}
              {user.emailConfirmed ? t("profile.emailConfirmed") : t("profile.emailNotConfirmed")}
            </div>
          </div>
        </div>
        <form onSubmit={saveProfile} className="space-y-4">
          {profileErr ? <Alert>{profileErr}</Alert> : null}
          {profileMsg ? <Alert kind="success">{profileMsg}</Alert> : null}
          <Field label={t("profile.displayName")}>
            <Input value={displayName} onChange={(e) => setDisplayName(e.target.value)} maxLength={40} required />
          </Field>
          <Field label={t("profile.avatarUrl")} hint={t("profile.avatarHint")}>
            <Input value={avatarUrl} onChange={(e) => setAvatarUrl(e.target.value)} placeholder="https://…" />
          </Field>
          <label className="flex items-center gap-2 text-sm text-slate-300">
            <input
              type="checkbox"
              checked={isPrivate}
              onChange={(e) => setIsPrivate(e.target.checked)}
              className="h-4 w-4 rounded border-slate-600 bg-slate-900"
            />
            {t("profile.privateProfile")}
          </label>
          <Button type="submit" disabled={savingProfile}>
            {savingProfile ? t("common.saving") : t("profile.saveProfile")}
          </Button>
        </form>
      </Card>

      <Card>
        <h2 className="mb-4 text-lg font-semibold text-white">{t("profile.changePassword")}</h2>
        <form onSubmit={changePassword} className="space-y-4">
          {pwErr ? <Alert>{pwErr}</Alert> : null}
          {pwMsg ? <Alert kind="success">{pwMsg}</Alert> : null}
          <Field label={t("profile.currentPassword")}>
            <Input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              required
              autoComplete="current-password"
            />
          </Field>
          <Field label={t("profile.newPassword")} hint={t("auth.passwordHint")}>
            <Input
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              required
              minLength={8}
              autoComplete="new-password"
            />
          </Field>
          <Button type="submit" disabled={savingPw}>
            {savingPw ? t("profile.updating") : t("profile.changePasswordCta")}
          </Button>
        </form>
      </Card>

      <Card>
        <h2 className="mb-4 text-lg font-semibold text-white">{t("profile.blockedUsers")}</h2>
        {blocked.isLoading ? (
          <Spinner />
        ) : blocked.data && blocked.data.length > 0 ? (
          <div className="space-y-2">
            {blocked.data.map((b) => (
              <div key={b.id} className="flex items-center justify-between rounded-lg bg-slate-800/50 px-3 py-2">
                <div className="flex items-center gap-2">
                  <Avatar name={b.displayName} url={b.avatarUrl} size={28} />
                  <span className="text-sm text-slate-200">{b.displayName}</span>
                </div>
                <Button variant="ghost" onClick={() => unblock(b.id)}>
                  {t("profile.unblock")}
                </Button>
              </div>
            ))}
          </div>
        ) : (
          <p className="text-sm text-slate-500">{t("profile.noBlocked")}</p>
        )}
      </Card>
    </div>
  );
}
