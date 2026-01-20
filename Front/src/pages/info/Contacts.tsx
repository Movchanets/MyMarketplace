import { useTranslation } from 'react-i18next'

export default function Contacts() {
  const { t } = useTranslation()

  return (
    <div className="grid gap-8 md:grid-cols-2">
      <div className="space-y-4">
        <h1 className="text-2xl font-semibold text-foreground">{t('contact.title')}</h1>
        <p className="text-foreground-muted">{t('contact.description')}</p>

        <div className="space-y-2 text-sm text-foreground-muted">
          <p>{t('contact.email')}</p>
          <p>{t('contact.phone')}</p>
          <p>{t('contact.hours')}</p>
        </div>
      </div>

      <form className="card space-y-4">
        <div>
          <label className="mb-1 block text-sm text-foreground-muted">{t('contact.form.name_label')}</label>
          <input className="w-full rounded-md border border-foreground/20 bg-transparent px-3 py-2 text-foreground outline-none focus:border-brand" />
        </div>
        <div>
          <label className="mb-1 block text-sm text-foreground-muted">{t('contact.form.email_label')}</label>
          <input type="email" className="w-full rounded-md border border-foreground/20 bg-transparent px-3 py-2 text-foreground outline-none focus:border-brand" />
        </div>
        <div>
          <label className="mb-1 block text-sm text-foreground-muted">{t('contact.form.message_label')}</label>
          <textarea rows={5} className="w-full rounded-md border border-foreground/20 bg-transparent px-3 py-2 text-foreground outline-none focus:border-brand" />
        </div>
        <button className="btn-primary w-full md:w-auto">{t('contact.form.send')}</button>
      </form>
    </div>
  )
}
