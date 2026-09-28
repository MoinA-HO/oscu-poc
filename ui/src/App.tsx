import { Navigate, Route, Routes } from 'react-router-dom';
import { PageShell } from '@/components/hods';
import { CreateReferralPage } from '@/features/referrals/pages/CreateReferralPage';
import { DeleteReferralPage } from '@/features/referrals/pages/DeleteReferralPage';
import { EditReferralPage } from '@/features/referrals/pages/EditReferralPage';
import { ReferralListPage } from '@/features/referrals/pages/ReferralListPage';

export function App() {
  return (
    <PageShell serviceName="Referral and Case Management">
      <Routes>
        <Route path="/" element={<Navigate to="/referrals" replace />} />
        <Route path="/referrals" element={<ReferralListPage />} />
        <Route path="/referrals/new" element={<CreateReferralPage />} />
        <Route path="/referrals/:id/edit" element={<EditReferralPage />} />
        <Route path="/referrals/:id/delete" element={<DeleteReferralPage />} />
        <Route path="*" element={<NotFound />} />
      </Routes>
    </PageShell>
  );
}

function NotFound() {
  return (
    <>
      <h1 className="govuk-heading-l">Page not found</h1>
      <p className="govuk-body">If you typed the web address, check it is correct.</p>
    </>
  );
}
