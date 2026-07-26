import { test } from '@playwright/test';
import { captureDocsScreenshot, openFeatureForCapture } from './helpers';

/**
 * Manual documentation screenshot suite.
 * Prerequisites:
 * - SoloDevBoard running locally with a real GitHub PAT.
 * - DocsCapture:Enabled=true (public-only catalogues).
 * - PLAYWRIGHT_BASE_URL pointing at the app (default http://localhost:5080).
 */
const captures: ReadonlyArray<{ route: string; slug: string; file: string; title: string }> = [
  { route: '/', slug: 'dashboard', file: 'home.png', title: 'Home dashboard' },
  { route: '/audit-dashboard', slug: 'audit-dashboard', file: 'overview.png', title: 'Audit Dashboard' },
  { route: '/repositories', slug: 'repositories', file: 'overview.png', title: 'Repositories' },
  { route: '/labels', slug: 'label-manager', file: 'overview.png', title: 'Label Manager' },
  { route: '/migrate', slug: 'one-click-migration', file: 'overview.png', title: 'One-Click Migration' },
  { route: '/board-rules', slug: 'board-rules-visualiser', file: 'overview.png', title: 'Board Rules Visualiser' },
  { route: '/triage', slug: 'triage-ui', file: 'overview.png', title: 'Triage UI' },
  { route: '/workflows', slug: 'workflow-templates', file: 'overview.png', title: 'Workflow Templates' },
  { route: '/about', slug: 'about', file: 'overview.png', title: 'About' },
];

for (const capture of captures) {
  test(`capture ${capture.title}`, async ({ page }) => {
    await openFeatureForCapture(page, capture.route);
    await captureDocsScreenshot(page, capture.slug, capture.file);
  });
}

test('capture Appearance theme control in app bar', async ({ page }) => {
  await openFeatureForCapture(page, '/');
  // Theme toggle lives in the app bar on every page; capture home with the control visible.
  await captureDocsScreenshot(page, 'appearance', 'theme-toggle.png');
});
