import { useTranslation } from 'react-i18next'
import { LoginForm } from '@/features/auth/ui/LoginForm'

export function LoginRoute() {
  const { t } = useTranslation()
  return (
    <main className="relative grid min-h-screen overflow-hidden px-4 py-10 md:grid-cols-[1fr_minmax(360px,480px)] md:items-center md:px-10 lg:px-16">
      <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_20%_20%,rgb(47_125_115/0.18),transparent_34rem),radial-gradient(circle_at_84%_72%,rgb(241_197_107/0.28),transparent_30rem)]" />
      <section className="relative hidden max-w-2xl md:block">
        <span className="inline-flex rounded-full bg-white/70 px-4 py-2 text-sm font-bold text-[var(--primary-strong)] shadow-sm ring-1 ring-white">
          ADEEB V2
        </span>
        <h1 className="adeeb-brand mt-7 text-7xl font-black leading-none text-[var(--text)] lg:text-8xl">
          {t('loginTitle')}
        </h1>
        <p className="mt-5 max-w-lg text-lg leading-8 text-[var(--muted)]">{t('loginSubtitle')}</p>
      </section>

      <section className="relative mx-auto w-full max-w-md rounded-[2rem] border border-white/80 bg-white/82 p-5 shadow-[0_30px_80px_rgb(24_49_45/0.16)] backdrop-blur-xl sm:p-7">
        <div className="mb-7">
          <div className="mb-5 inline-grid h-13 w-13 place-items-center rounded-[1.35rem] bg-[linear-gradient(135deg,var(--primary),#8ab5ad)] text-xl font-black text-white shadow-[0_18px_40px_rgb(47_125_115/0.28)]">
            A
          </div>
          <h2 className="adeeb-brand text-5xl font-black leading-none tracking-normal text-[var(--text)]">{t('loginTitle')}</h2>
          <p className="mt-3 text-sm leading-6 text-[var(--muted)]">{t('loginSubtitle')}</p>
        </div>
        <LoginForm />
      </section>
    </main>
  )
}
