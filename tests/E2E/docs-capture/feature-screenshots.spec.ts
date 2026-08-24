import { test } from '@playwright/test';
import {
  captureDocsScreenshot,
  openFeatureForCapture,
  prepareAuditDashboardForCapture,
  prepareBoardRulesForCapture,
  prepareLabelManagerForCapture,
  prepareMigrationForCapture,
  preparePlanningBacklogForCapture,
  preparePlanningDailyFocusForCapture,
  preparePlanningIterationForCapture,
  preparePlanningReposForCapture,
  prepareRepositoriesForCapture,
  prepareTriageForCapture,
  prepareActionsTemplatesForCapture,
} from './helpers';

/**
 * Manual documentation screenshot suite.
 * Prerequisites:
 * - SoloDevBoard running locally with a real GitHub PAT.
 * - DocsCapture:Enabled=true (public-only catalogues).
 * - PLAYWRIGHT_BASE_URL pointing at the app (default http://localhost:5080).
 *
 * Composition rules: see plan/DOCS_STRATEGY.md#screenshot-composition.
 */
const loadedStateCaptures: ReadonlyArray<{
  title: string;
  slug: string;
  file: string;
  prepare: (page: import('@playwright/test').Page) => Promise<void>;
}> = [
  {
    title: 'Audit Dashboard with loaded KPI summary',
    slug: 'audit-dashboard',
    file: 'overview.png',
    prepare: prepareAuditDashboardForCapture,
  },
  {
    title: 'Repositories with populated grid',
    slug: 'repositories',
    file: 'overview.png',
    prepare: prepareRepositoriesForCapture,
  },
  {
    title: 'Label Manager with loaded labels grid',
    slug: 'label-manager',
    file: 'overview.png',
    prepare: prepareLabelManagerForCapture,
  },
  {
    title: 'One-Click Migration with example repository selected',
    slug: 'one-click-migration',
    file: 'overview.png',
    prepare: prepareMigrationForCapture,
  },
  {
    title: 'Board Rules Visualiser with board context loaded',
    slug: 'board-rules-visualiser',
    file: 'overview.png',
    prepare: prepareBoardRulesForCapture,
  },
  {
    title: 'Triage UI with active session',
    slug: 'triage-ui',
    file: 'overview.png',
    prepare: prepareTriageForCapture,
  },
  {
    title: 'Actions Templates with template detail open',
    slug: 'actions-templates',
    file: 'overview.png',
    prepare: prepareActionsTemplatesForCapture,
  },
  {
    title: 'Planning — Daily Focus with board occupancy',
    slug: 'planning',
    file: 'daily-focus.png',
    prepare: preparePlanningDailyFocusForCapture,
  },
  {
    title: 'Planning Backlog Review with urgency panels',
    slug: 'planning',
    file: 'backlog.png',
    prepare: preparePlanningBacklogForCapture,
  },
  {
    title: 'Planning Iteration with Up Next and candidates',
    slug: 'planning',
    file: 'iteration.png',
    prepare: preparePlanningIterationForCapture,
  },
  {
    title: 'Planning Repo Management thresholds and participation',
    slug: 'planning',
    file: 'repos.png',
    prepare: preparePlanningReposForCapture,
  },
];

for (const capture of loadedStateCaptures) {
  test(`capture ${capture.title}`, async ({ page }) => {
    await capture.prepare(page);
    await captureDocsScreenshot(page, capture.slug, capture.file);
  });
}

test('capture Home dashboard', async ({ page }) => {
  await openFeatureForCapture(page, '/');
  await captureDocsScreenshot(page, 'dashboard', 'home.png');
});

test('capture About page', async ({ page }) => {
  await openFeatureForCapture(page, '/about');
  await captureDocsScreenshot(page, 'about', 'overview.png');
});

test('capture Appearance theme control in app bar', async ({ page }) => {
  await openFeatureForCapture(page, '/');
  // Theme toggle lives in the app bar on every page; capture home with the control visible.
  await captureDocsScreenshot(page, 'appearance', 'theme-toggle.png');
});
