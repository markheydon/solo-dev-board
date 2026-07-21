import { expect, type Page } from '@playwright/test';

export const featureRoutes = [
  { navLabel: 'Audit Dashboard', path: '/audit-dashboard', title: /Audit Dashboard/ },
  { navLabel: 'Repositories', path: '/repositories', title: /Repositories/ },
  { navLabel: 'Migrate', path: '/migrate', title: /One-Click Migration/ },
  { navLabel: 'Labels', path: '/labels', title: /Label Manager/ },
  { navLabel: 'Board Rules', path: '/board-rules', title: /Board Rules Visualiser/ },
  { navLabel: 'Triage', path: '/triage', title: /Triage UI/ },
  { navLabel: 'Workflows', path: '/workflows', title: /Workflow Templates/ },
] as const;

async function ensureDrawerOpen(page: Page): Promise<void> {
  const drawer = page.locator('#nav-drawer');
  const firstNavLink = drawer.getByRole('link').first();

  if (!(await firstNavLink.isVisible())) {
    await page.getByRole('banner').getByRole('button').first().click();
    await expect(firstNavLink).toBeVisible();
  }
}

export async function navigateViaDrawer(page: Page, label: string): Promise<void> {
  await ensureDrawerOpen(page);

  const link = page.locator('#nav-drawer').getByRole('link', { name: label, exact: true });
  await expect(link).toBeVisible();

  // MudBlazor drawer transforms can leave links outside Playwright's clickable viewport.
  await link.evaluate((element) => (element as HTMLAnchorElement).click());
}
