import { Link } from 'react-router-dom'
import { cn } from '@/shared/lib/cn'

export function AdeebBrand({ to = '/', compact = false, inverse = false, className }: { to?: string; compact?: boolean; inverse?: boolean; className?: string }) {
  return (
    <Link to={to} className={cn('inline-flex items-center gap-3 no-underline', inverse ? 'text-white' : 'text-[#111b3d]', className)}>
      <span className={cn('grid shrink-0 place-items-center rounded-lg bg-[#5146f0] font-black text-white shadow-[0_8px_20px_rgb(81_70_240/0.22)]', compact ? 'h-9 w-9 text-lg' : 'h-11 w-11 text-xl')}>A</span>
      <strong className={cn('font-black tracking-normal', compact ? 'text-xl' : 'text-2xl')}>ADEEB</strong>
    </Link>
  )
}
