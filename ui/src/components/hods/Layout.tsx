import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

type HeaderProps = {
  serviceName: string;
  homeHref?: string;
};

export function Header({ serviceName, homeHref = '/' }: HeaderProps) {
  return (
    <header className="govuk-header" data-module="govuk-header">
      <div className="govuk-header__container govuk-width-container">
        <div className="govuk-header__logo">
          <Link to={homeHref} className="govuk-header__link govuk-header__link--homepage">
            <span className="govuk-header__logotype-text">Home Office</span>
          </Link>
        </div>
        <div className="govuk-header__content">
          <Link to={homeHref} className="govuk-header__link govuk-header__service-name">
            {serviceName}
          </Link>
        </div>
      </div>
    </header>
  );
}

export function Footer() {
  return (
    <footer className="govuk-footer">
      <div className="govuk-width-container">
        <div className="govuk-footer__meta">
          <div className="govuk-footer__meta-item govuk-footer__meta-item--grow">
            <span className="govuk-footer__licence-description">
              All content is available under the Open Government Licence v3.0, except where otherwise stated
            </span>
          </div>
        </div>
      </div>
    </footer>
  );
}

/**
 * The skip link is not decoration: it is a WCAG 2.4.1 requirement, letting
 * keyboard and screen reader users jump past the header to the content.
 */
export function SkipLink() {
  return (
    <a href="#main-content" className="govuk-skip-link" data-module="govuk-skip-link">
      Skip to main content
    </a>
  );
}

export function PageShell({ serviceName, children }: { serviceName: string; children: ReactNode }) {
  return (
    <>
      <SkipLink />
      <Header serviceName={serviceName} />
      <div className="govuk-width-container">
        <main className="govuk-main-wrapper" id="main-content" role="main">
          {children}
        </main>
      </div>
      <Footer />
    </>
  );
}

export function BackLink({ to, children = 'Back' }: { to: string; children?: ReactNode }) {
  return (
    <Link to={to} className="govuk-back-link">
      {children}
    </Link>
  );
}
