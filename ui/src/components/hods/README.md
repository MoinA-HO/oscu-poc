# Design system adapter layer

Every design-system component the app uses is re-exported from this folder.
Feature code imports from `@/components/hods` and **never** from a design
system package directly.

## Why

`@ukhoautomation/hods-react` lives on a private Azure DevOps feed that is not
available yet. Rather than block the build on it, these adapters render the
underlying **GOV.UK Design System** markup using the public `govuk-frontend`
package. The Home Office Design System is a themed layer over GOV.UK Frontend,
so the class names and DOM structure already match.

The practical consequences:

- The UI runs, builds and is testable today with no feed access.
- The swap later touches this folder only. No page, hook or test changes.
- Accessibility behaviour (labels, error summaries, focus management, hint and
  error `aria-describedby` wiring) is inherited from the design system rather
  than reinvented per form.

## Swapping in the real components

Once the feed is reachable:

1. Authenticate against the `ACReact` feed (see `.npmrc`).
2. `npm install @ukhoautomation/hods-react`
3. Import the stylesheet in `src/main.tsx`:
   `import '@ukhoautomation/hods-react/dist/hods-react.css';`
4. Replace each adapter body with a re-export, keeping the exported name and
   prop names identical. For example `Header.tsx` becomes:

   ```tsx
   export { Header } from '@ukhoautomation/hods-react';
   ```

   Where the real component's prop names differ, keep the adapter and map the
   props inside it. That is the entire point of this layer: the mismatch is
   absorbed in one file instead of rippling through every page.

5. Run `npm test`. The component tests here assert on accessible roles and
   label text rather than class names, so they should keep passing across the
   swap — if one fails, the real component genuinely behaves differently and
   you want to know.

## Rules

- No business logic in this folder. These are presentational only.
- No feature-specific props (nothing called `referral*`).
- Every interactive component must expose a label and an error state.
