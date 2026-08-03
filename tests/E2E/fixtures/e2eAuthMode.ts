/** Active authentication mode for Playwright end-to-end tests. */
export type E2eAuthMode = 'pat' | 'hosted';

/** Returns the configured E2E auth mode (defaults to PAT). */
export function getE2eAuthMode(): E2eAuthMode {
  const mode = process.env.E2E_AUTH_MODE?.toLowerCase();
  return mode === 'hosted' ? 'hosted' : 'pat';
}

/** Returns true when the suite is running in hosted sign-in mode. */
export function isHostedE2eMode(): boolean {
  return getE2eAuthMode() === 'hosted';
}

/** Returns true when the suite is running in PAT mode. */
export function isPatE2eMode(): boolean {
  return getE2eAuthMode() === 'pat';
}
