-- Phase 8G-a: read-only auth/session pre-flight audit (do not modify data)

\echo '========== TABLE COUNTS =========='
SELECT 'platform_auth_sessions' AS tbl, COUNT(*) AS cnt FROM platform_auth_sessions
UNION ALL SELECT 'platform_refresh_tokens', COUNT(*) FROM platform_refresh_tokens
UNION ALL SELECT 'platform_login_audits', COUNT(*) FROM platform_login_audits
UNION ALL SELECT 'platform_password_reset_tokens', COUNT(*) FROM platform_password_reset_tokens;

\echo '========== 1. platform_auth_sessions =========='

\echo '-- 1a platform_user_id nulls'
SELECT COUNT(*) AS cnt FROM platform_auth_sessions WHERE platform_user_id IS NULL;
SELECT id, status, created_at FROM platform_auth_sessions WHERE platform_user_id IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 1b status says ACTIVE but revoked_at is not null (drift)'
SELECT COUNT(*) AS cnt FROM platform_auth_sessions WHERE status = 'ACTIVE' AND revoked_at IS NOT NULL;
SELECT id, platform_user_id, status, revoked_at, revoke_reason, created_at
FROM platform_auth_sessions WHERE status = 'ACTIVE' AND revoked_at IS NOT NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 1c status says REVOKED but revoked_at is null (drift)'
SELECT COUNT(*) AS cnt FROM platform_auth_sessions WHERE status = 'REVOKED' AND revoked_at IS NULL;
SELECT id, platform_user_id, status, revoked_at, revoke_reason, created_at
FROM platform_auth_sessions WHERE status = 'REVOKED' AND revoked_at IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 1d invalid status values'
SELECT COUNT(*) AS cnt FROM platform_auth_sessions WHERE status NOT IN ('ACTIVE', 'EXPIRED', 'REVOKED');
SELECT id, status, created_at FROM platform_auth_sessions WHERE status NOT IN ('ACTIVE', 'EXPIRED', 'REVOKED') LIMIT 20;

\echo '-- 1e session_token_hash null or empty'
SELECT COUNT(*) AS cnt FROM platform_auth_sessions WHERE session_token_hash IS NULL OR BTRIM(session_token_hash) = '';
SELECT id, status, created_at FROM platform_auth_sessions WHERE session_token_hash IS NULL OR BTRIM(session_token_hash) = '' LIMIT 20;

\echo '-- 1f duplicate session_token_hash'
SELECT session_token_hash, COUNT(*) AS cnt
FROM platform_auth_sessions
WHERE session_token_hash IS NOT NULL AND BTRIM(session_token_hash) <> ''
GROUP BY session_token_hash
HAVING COUNT(*) > 1
ORDER BY cnt DESC
LIMIT 20;

\echo '========== 2. platform_refresh_tokens =========='

\echo '-- 2a platform_user_id nulls'
SELECT COUNT(*) AS cnt FROM platform_refresh_tokens WHERE platform_user_id IS NULL;
SELECT id, status, platform_auth_session_id, created_at FROM platform_refresh_tokens WHERE platform_user_id IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 2b token_family_id nulls'
SELECT COUNT(*) AS cnt FROM platform_refresh_tokens WHERE token_family_id IS NULL;
SELECT id, status, token_family_id, created_at FROM platform_refresh_tokens WHERE token_family_id IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 2c USED with used_at null'
SELECT COUNT(*) AS cnt FROM platform_refresh_tokens WHERE status = 'USED' AND used_at IS NULL;
SELECT id, token_family_id, replaced_by_token_id, created_at FROM platform_refresh_tokens WHERE status = 'USED' AND used_at IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 2d USED with replaced_by_token_id null but younger sibling in same family exists'
SELECT COUNT(*) AS cnt
FROM platform_refresh_tokens t
WHERE t.status = 'USED'
  AND t.replaced_by_token_id IS NULL
  AND EXISTS (
    SELECT 1 FROM platform_refresh_tokens n
    WHERE n.token_family_id = t.token_family_id
      AND n.created_at > t.created_at
  );
SELECT t.id, t.token_family_id, t.status, t.replaced_by_token_id, t.created_at
FROM platform_refresh_tokens t
WHERE t.status = 'USED'
  AND t.replaced_by_token_id IS NULL
  AND EXISTS (
    SELECT 1 FROM platform_refresh_tokens n
    WHERE n.token_family_id = t.token_family_id AND n.created_at > t.created_at
  )
ORDER BY t.created_at DESC
LIMIT 20;

\echo '-- 2e ACTIVE with revoked_at not null'
SELECT COUNT(*) AS cnt FROM platform_refresh_tokens WHERE status = 'ACTIVE' AND revoked_at IS NOT NULL;
SELECT id, status, revoked_at, created_at FROM platform_refresh_tokens WHERE status = 'ACTIVE' AND revoked_at IS NOT NULL LIMIT 20;

\echo '-- 2f REVOKED with revoked_at null'
SELECT COUNT(*) AS cnt FROM platform_refresh_tokens WHERE status = 'REVOKED' AND revoked_at IS NULL;
SELECT id, status, token_family_id, created_at FROM platform_refresh_tokens WHERE status = 'REVOKED' AND revoked_at IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 2g orphan platform_auth_session_id'
SELECT COUNT(*) AS cnt
FROM platform_refresh_tokens t
WHERE t.platform_auth_session_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM platform_auth_sessions s WHERE s.id = t.platform_auth_session_id);
SELECT t.id, t.platform_auth_session_id, t.status, t.created_at
FROM platform_refresh_tokens t
WHERE t.platform_auth_session_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM platform_auth_sessions s WHERE s.id = t.platform_auth_session_id)
LIMIT 20;

\echo '-- 2h orphan replaced_by_token_id'
SELECT COUNT(*) AS cnt
FROM platform_refresh_tokens t
WHERE t.replaced_by_token_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM platform_refresh_tokens r WHERE r.id = t.replaced_by_token_id);
SELECT t.id, t.replaced_by_token_id, t.status, t.created_at
FROM platform_refresh_tokens t
WHERE t.replaced_by_token_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM platform_refresh_tokens r WHERE r.id = t.replaced_by_token_id)
LIMIT 20;

\echo '-- 2i duplicate replaced_by_token_id'
SELECT replaced_by_token_id, COUNT(*) AS cnt
FROM platform_refresh_tokens
WHERE replaced_by_token_id IS NOT NULL
GROUP BY replaced_by_token_id
HAVING COUNT(*) > 1
ORDER BY cnt DESC
LIMIT 20;

\echo '-- 2j invalid status values'
SELECT COUNT(*) AS cnt FROM platform_refresh_tokens WHERE status NOT IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED');
SELECT id, status, created_at FROM platform_refresh_tokens WHERE status NOT IN ('ACTIVE', 'USED', 'EXPIRED', 'REVOKED') LIMIT 20;

\echo '========== 3. platform_login_audits =========='

\echo '-- 3a login_status nulls'
SELECT COUNT(*) AS cnt FROM platform_login_audits WHERE login_status IS NULL;
SELECT id, login_result, login_status, created_at FROM platform_login_audits WHERE login_status IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 3b attempted_at nulls'
SELECT COUNT(*) AS cnt FROM platform_login_audits WHERE attempted_at IS NULL;
SELECT id, login_result, created_at FROM platform_login_audits WHERE attempted_at IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 3c authentication_method nulls'
SELECT COUNT(*) AS cnt FROM platform_login_audits WHERE authentication_method IS NULL OR BTRIM(authentication_method) = '';
SELECT id, login_result, authentication_method, created_at FROM platform_login_audits WHERE authentication_method IS NULL OR BTRIM(authentication_method) = '' ORDER BY created_at DESC LIMIT 20;

\echo '-- 3d login_result/login_status mismatch'
SELECT COUNT(*) AS cnt FROM platform_login_audits WHERE login_status IS NOT NULL AND login_result <> login_status;
SELECT id, login_result, login_status, created_at FROM platform_login_audits WHERE login_status IS NOT NULL AND login_result <> login_status ORDER BY created_at DESC LIMIT 20;

\echo '-- 3e invalid login_status values'
SELECT COUNT(*) AS cnt FROM platform_login_audits WHERE login_status IS NOT NULL AND login_status NOT IN ('SUCCESS', 'FAILED', 'LOCKED');
SELECT id, login_status, login_result, created_at FROM platform_login_audits WHERE login_status IS NOT NULL AND login_status NOT IN ('SUCCESS', 'FAILED', 'LOCKED') LIMIT 20;

\echo '-- 3f orphan platform_auth_session_id'
SELECT COUNT(*) AS cnt
FROM platform_login_audits a
WHERE a.platform_auth_session_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM platform_auth_sessions s WHERE s.id = a.platform_auth_session_id);
SELECT a.id, a.platform_auth_session_id, a.login_result, a.created_at
FROM platform_login_audits a
WHERE a.platform_auth_session_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM platform_auth_sessions s WHERE s.id = a.platform_auth_session_id)
LIMIT 20;

\echo '========== 4. platform_password_reset_tokens =========='

\echo '-- 4a requested_at nulls'
SELECT COUNT(*) AS cnt FROM platform_password_reset_tokens WHERE requested_at IS NULL;
SELECT id, status, platform_user_id, created_at FROM platform_password_reset_tokens WHERE requested_at IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 4b expires_at nulls'
SELECT COUNT(*) AS cnt FROM platform_password_reset_tokens WHERE expires_at IS NULL;
SELECT id, status, requested_at, created_at FROM platform_password_reset_tokens WHERE expires_at IS NULL ORDER BY created_at DESC LIMIT 20;

\echo '-- 4c expires_at <= requested_at'
SELECT COUNT(*) AS cnt FROM platform_password_reset_tokens WHERE expires_at IS NOT NULL AND requested_at IS NOT NULL AND expires_at <= requested_at;
SELECT id, status, requested_at, expires_at FROM platform_password_reset_tokens WHERE expires_at IS NOT NULL AND requested_at IS NOT NULL AND expires_at <= requested_at LIMIT 20;

\echo '-- 4d USED with used_at null'
SELECT COUNT(*) AS cnt FROM platform_password_reset_tokens WHERE status = 'USED' AND used_at IS NULL;
SELECT id, status, requested_at, created_at FROM platform_password_reset_tokens WHERE status = 'USED' AND used_at IS NULL LIMIT 20;

\echo '-- 4e REVOKED with revoked_at null'
SELECT COUNT(*) AS cnt FROM platform_password_reset_tokens WHERE status = 'REVOKED' AND revoked_at IS NULL;
SELECT id, status, created_at FROM platform_password_reset_tokens WHERE status = 'REVOKED' AND revoked_at IS NULL LIMIT 20;

\echo '-- 4f invalid status values'
SELECT COUNT(*) AS cnt FROM platform_password_reset_tokens WHERE status NOT IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED');
SELECT id, status, created_at FROM platform_password_reset_tokens WHERE status NOT IN ('PENDING', 'USED', 'EXPIRED', 'REVOKED') LIMIT 20;

\echo '========== 5. 8G-b/c readiness signals =========='

\echo '-- Active sessions per user (revoked_at IS NULL)'
SELECT platform_user_id, COUNT(*) AS active_sessions
FROM platform_auth_sessions
WHERE revoked_at IS NULL AND platform_user_id IS NOT NULL
GROUP BY platform_user_id
HAVING COUNT(*) > 1
ORDER BY active_sessions DESC
LIMIT 20;

\echo '-- Active refresh tokens per user (8G-c preview)'
SELECT platform_user_id, COUNT(*) AS active_tokens
FROM platform_refresh_tokens
WHERE status = 'ACTIVE' AND platform_user_id IS NOT NULL
GROUP BY platform_user_id
HAVING COUNT(*) > 1
ORDER BY active_tokens DESC
LIMIT 20;
