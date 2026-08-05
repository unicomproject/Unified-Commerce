-- Run only after EF migrations on a disposable database named oneverz_flow4_e2e_*.
-- psql variables required: environment, database_name, database_role, marker_nonce, marker_expires_at.
\set ON_ERROR_STOP on
DO $$
BEGIN
  IF current_database() !~ '^oneverz_flow4_e2e_[a-z0-9_]{8,64}$' THEN
    RAISE EXCEPTION 'Refusing Flow 4 control schema on non-isolated database';
  END IF;
END $$;

CREATE SCHEMA flow4_test_control AUTHORIZATION CURRENT_USER;
REVOKE ALL ON SCHEMA flow4_test_control FROM PUBLIC;

CREATE TABLE flow4_test_control.environment_marker (
  marker_id integer PRIMARY KEY CHECK (marker_id = 1),
  environment text NOT NULL CHECK (environment IN ('Test', 'E2E')),
  database_name text NOT NULL,
  database_role text NOT NULL,
  marker_nonce text NOT NULL,
  expires_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE flow4_test_control.fixture_runs (
  run_id uuid PRIMARY KEY,
  cleanup_handle_hash char(64) NOT NULL,
  fixture_set_version text NOT NULL,
  status text NOT NULL CHECK (status IN ('ACTIVE', 'CLEANED')),
  created_at timestamptz NOT NULL,
  expires_at timestamptz NOT NULL,
  cleaned_at timestamptz
);
CREATE TABLE flow4_test_control.fixture_resources (
  run_id uuid NOT NULL REFERENCES flow4_test_control.fixture_runs(run_id),
  scenario text NOT NULL,
  resource_type text NOT NULL,
  resource_id uuid NOT NULL,
  created_at timestamptz NOT NULL,
  PRIMARY KEY (run_id, resource_type, resource_id)
);
REVOKE ALL ON ALL TABLES IN SCHEMA flow4_test_control FROM PUBLIC;
GRANT USAGE ON SCHEMA flow4_test_control TO :"database_role";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA flow4_test_control TO :"database_role";

INSERT INTO flow4_test_control.environment_marker
  (marker_id, environment, database_name, database_role, marker_nonce, expires_at)
VALUES (1, :'environment', :'database_name', :'database_role', :'marker_nonce', :'marker_expires_at');

-- Approved, fixed-purpose E2E authorization profiles. These are database-bootstrap data,
-- not caller-selectable fixture inputs. The super-administrator role is production seed data.
INSERT INTO platform_roles (id, role_code, name, description, is_system_role, status, created_at, updated_at)
VALUES
 ('f4000000-0000-4000-8000-000000000001', 'billing_viewer_dev', 'Billing Viewer E2E', 'Flow 4 isolated E2E role.', false, 'ACTIVE', now(), now()),
 ('f4000000-0000-4000-8000-000000000002', 'platform_ops_no_billing_dev', 'Platform Ops No Billing E2E', 'Flow 4 isolated E2E role.', false, 'ACTIVE', now(), now())
ON CONFLICT (role_code) DO NOTHING;

INSERT INTO platform_role_permissions
  (id, platform_role_id, platform_permission_id, description, granted_at, created_at, updated_at)
SELECT
  CASE p.permission_code
    WHEN 'platform.billing.view' THEN 'f4000000-0000-4000-8000-000000000011'::uuid
    WHEN 'platform.dashboard.view' THEN 'f4000000-0000-4000-8000-000000000012'::uuid
  END,
  'f4000000-0000-4000-8000-000000000001'::uuid, p.id, 'Flow 4 isolated E2E grant.', now(), now(), now()
FROM platform_permissions p WHERE p.permission_code IN ('platform.billing.view', 'platform.dashboard.view')
ON CONFLICT DO NOTHING;

INSERT INTO platform_role_permissions
  (id, platform_role_id, platform_permission_id, description, granted_at, created_at, updated_at)
SELECT
  CASE p.permission_code
    WHEN 'platform.dashboard.view' THEN 'f4000000-0000-4000-8000-000000000021'::uuid
    WHEN 'platform.tenants.view' THEN 'f4000000-0000-4000-8000-000000000022'::uuid
  END,
  'f4000000-0000-4000-8000-000000000002'::uuid, p.id, 'Flow 4 isolated E2E grant.', now(), now(), now()
FROM platform_permissions p WHERE p.permission_code IN ('platform.dashboard.view', 'platform.tenants.view')
ON CONFLICT DO NOTHING;
