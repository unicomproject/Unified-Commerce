\echo '=== 8G-c PREFLIGHT ==='

\echo '1 duplicate ACTIVE per platform_auth_session_id'
SELECT COUNT(*) AS violation_groups FROM (
  SELECT platform_auth_session_id FROM platform_refresh_tokens
  WHERE status = 'ACTIVE' GROUP BY platform_auth_session_id HAVING COUNT(*) > 1
) d;

\echo '1b offenders'
SELECT platform_auth_session_id, COUNT(*) AS active_count
FROM platform_refresh_tokens WHERE status = 'ACTIVE'
GROUP BY platform_auth_session_id HAVING COUNT(*) > 1;

\echo '2 duplicate ACTIVE per token_family_id'
SELECT COUNT(*) AS violation_groups FROM (
  SELECT token_family_id FROM platform_refresh_tokens
  WHERE status = 'ACTIVE' GROUP BY token_family_id HAVING COUNT(*) > 1
) d;

\echo '2b offenders'
SELECT token_family_id, COUNT(*) AS active_count
FROM platform_refresh_tokens WHERE status = 'ACTIVE'
GROUP BY token_family_id HAVING COUNT(*) > 1;

\echo '3 duplicate replaced_by_token_id'
SELECT COUNT(*) AS violation_groups FROM (
  SELECT replaced_by_token_id FROM platform_refresh_tokens
  WHERE replaced_by_token_id IS NOT NULL GROUP BY replaced_by_token_id HAVING COUNT(*) > 1
) d;

\echo '3b offenders'
SELECT replaced_by_token_id, COUNT(*) AS cnt
FROM platform_refresh_tokens WHERE replaced_by_token_id IS NOT NULL
GROUP BY replaced_by_token_id HAVING COUNT(*) > 1;

\echo '4 orphan replaced_by_token_id'
SELECT COUNT(*) AS cnt FROM platform_refresh_tokens t
WHERE replaced_by_token_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM platform_refresh_tokens r WHERE r.id = t.replaced_by_token_id);

\echo '5 active sessions (revoked_at IS NULL)'
SELECT COUNT(*) FROM platform_auth_sessions WHERE revoked_at IS NULL;

\echo '6 active tokens'
SELECT COUNT(*) FROM platform_refresh_tokens WHERE status = 'ACTIVE';

\echo '7 active session (revoked_at IS NULL) without ACTIVE token'
SELECT COUNT(*) FROM platform_auth_sessions s
WHERE s.revoked_at IS NULL AND NOT EXISTS (
  SELECT 1 FROM platform_refresh_tokens t WHERE t.platform_auth_session_id = s.id AND t.status = 'ACTIVE');

\echo '8 ACTIVE token without active session (revoked_at IS NULL)'
SELECT COUNT(*) FROM platform_refresh_tokens t
WHERE t.status = 'ACTIVE' AND NOT EXISTS (
  SELECT 1 FROM platform_auth_sessions s WHERE s.id = t.platform_auth_session_id AND s.revoked_at IS NULL);

\echo '9 multi active sessions per user (allowed)'
SELECT platform_user_id, COUNT(*) AS active_sessions
FROM platform_auth_sessions WHERE revoked_at IS NULL
GROUP BY platform_user_id HAVING COUNT(*) > 1;
