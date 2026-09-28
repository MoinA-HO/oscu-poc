/**
 * govuk-frontend ships as ES modules with no bundled TypeScript declarations.
 *
 * Only the entry points this project actually calls are declared, rather than
 * a blanket `declare module 'govuk-frontend'` that would type the whole
 * package as `any` and hide genuine mistakes.
 */
declare module 'govuk-frontend' {
  /**
   * Initialises every design system component found in the given scope,
   * providing the progressive enhancement behaviour (skip link focus, error
   * summary focus management, character counts).
   */
  export function initAll(config?: { scope?: Element | Document }): void;
}
