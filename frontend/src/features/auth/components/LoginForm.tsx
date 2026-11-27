import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Button } from '@/components/ui/Button'
import { FormError } from '@/components/ui/FormError'
import { TextField } from '@/components/ui/TextField'
import { loginSchema, type LoginValues } from '../lib/loginSchema'

interface LoginFormProps {
  onSubmit: (values: LoginValues) => void
  busy: boolean
  error?: string | null
}

export function LoginForm({ onSubmit, busy, error }: LoginFormProps) {
  const [showPassword, setShowPassword] = useState(false)
  const { register, handleSubmit, formState: { errors } } = useForm<LoginValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { userName: '', password: '' },
  })

  return (
    <form onSubmit={handleSubmit(onSubmit)} noValidate className="flex flex-col gap-5">
      <TextField
        label="Tên đăng nhập"
        autoComplete="username"
        autoCapitalize="none"
        autoCorrect="off"
        spellCheck={false}
        autoFocus
        error={errors.userName?.message}
        {...register('userName')}
      />
      <TextField
        label="Mật khẩu"
        type={showPassword ? 'text' : 'password'}
        autoComplete="current-password"
        error={errors.password?.message}
        trailing={
          <button
            type="button"
            onClick={() => setShowPassword((v) => !v)}
            aria-pressed={showPassword}
            className="h-full shrink-0 rounded-r-(--radius-control) px-4 text-sm font-medium text-ink-muted hover:text-ink"
          >
            {showPassword ? 'Ẩn' : 'Hiện'}
          </button>
        }
        {...register('password')}
      />
      <FormError message={error} />
      <Button type="submit" size="lg" busy={busy} className="mt-1 w-full">
        Đăng nhập
      </Button>
    </form>
  )
}
