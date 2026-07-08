import { Badge } from '@/shared/ui/Badge'
import { useTranslation } from 'react-i18next'

export function StatusBadge({ status }: { status: number }) {
  const { t } = useTranslation()
  if (status === 1) return <Badge tone="success">{t('statusActive')}</Badge>
  if (status === 2) return <Badge tone="warning">{t('statusArchived')}</Badge>
  return <Badge>{t('statusDraft')}</Badge>
}
