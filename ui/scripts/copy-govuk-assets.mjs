import { cp, mkdir, rm } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * Copies the govuk-frontend fonts and images into public/assets.
 *
 * The stylesheet references them at /assets/..., so without this the site
 * renders in a fallback font and the crown logo and favicons 404. Run from the
 * dev and build scripts rather than postinstall, so a plain `npm ci` in CI
 * does not depend on it.
 */
const here = dirname(fileURLToPath(import.meta.url));
const source = resolve(here, '../node_modules/govuk-frontend/dist/govuk/assets');
const destination = resolve(here, '../public/assets');

await rm(destination, { recursive: true, force: true });
await mkdir(destination, { recursive: true });
await cp(source, destination, { recursive: true });

console.log(`Copied govuk-frontend assets to ${destination}`);
