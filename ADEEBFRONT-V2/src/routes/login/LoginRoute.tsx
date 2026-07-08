import { LoginForm } from '@/features/auth/ui/LoginForm'

export function LoginRoute() {
  return (
    <main className="grid min-h-screen place-items-center px-4 py-10">
      <section className="w-full max-w-md rounded-lg border border-[var(--border)] bg-[var(--surface)] p-8 shadow-sm">
        <div className="mb-8">
          <div className="mb-4 inline-grid h-11 w-11 place-items-center rounded-md bg-[var(--primary)] text-lg font-black text-white">
            A
          </div>
          <h1 className="text-2xl font-bold tracking-normal">ADEEB Admin</h1>
          <p className="mt-2 text-sm text-[var(--muted)]">Ба панели идоракунии V2 ворид шавед.</p>
        </div>
        <LoginForm />
      </section>
    </main>
  )
}
