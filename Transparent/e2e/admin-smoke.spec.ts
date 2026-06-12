import { expect, test } from '@playwright/test'

test('admin login, create brand, edit brand', async ({ page }) => {
  const adminUsername = process.env.E2E_ADMIN_USERNAME
  const adminPassword = process.env.E2E_ADMIN_PASSWORD
  test.skip(!adminUsername || !adminPassword, 'Set E2E_ADMIN_USERNAME and E2E_ADMIN_PASSWORD to run admin smoke tests.')
  const uniqueId = Date.now()
  const createdBrandName = `Smoke Brand ${uniqueId}`
  const updatedBrandName = `${createdBrandName} Updated`

  await page.goto('/admin/login')

  await page.locator('#admin-username').fill(adminUsername)
  await page.locator('#admin-password').fill(adminPassword)
  await page.getByRole('button', { name: 'Sign in' }).click()

  await expect(page).toHaveURL(/\/admin\/dashboard$/)

  await page.getByRole('button', { name: 'Add new brand' }).click()
  await expect(page).toHaveURL(/\/admin\/brands\/new$/)

  await page.locator('.admin-brand-form-field input').first().fill(createdBrandName)
  await page.locator('.admin-brand-form-description-control').fill('E2E smoke test brand')

  await page.getByRole('button', { name: 'Create brand' }).click()
  await expect(page).toHaveURL(/\/admin\/dashboard$/)

  const createdBrandCard = page.locator('.admin-brand-card').filter({ hasText: createdBrandName }).first()
  await expect(createdBrandCard).toBeVisible()
  await createdBrandCard.getByRole('button', { name: 'Edit' }).click()

  await expect(page).toHaveURL(/\/admin\/brands\/\d+\/edit$/)
  await page.locator('.admin-brand-form-field input').first().fill(updatedBrandName)
  await page.getByRole('button', { name: 'Save changes' }).click()

  await expect(page).toHaveURL(/\/admin\/dashboard$/)
  const updatedBrandCard = page.locator('.admin-brand-card').filter({ hasText: updatedBrandName }).first()
  await expect(updatedBrandCard).toBeVisible()
})
