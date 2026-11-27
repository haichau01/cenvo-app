import { useNavigate } from 'react-router'
import { toast } from '@/components/ui/toastStore'
import { useDeleteDocument } from '../api/documents'

/** Drafts are deleted outright (they never touched stock); posted documents are cancelled instead. */
export function useDeleteDocumentFlow(documentId: string | null) {
  const navigate = useNavigate()
  const remove = useDeleteDocument()
  return {
    busy: remove.isPending,
    remove: () => {
      if (!documentId || !window.confirm('Xóa phiếu nháp này? Phiếu nháp chưa ảnh hưởng tồn kho.')) return
      remove.mutate(documentId, {
        onSuccess: () => {
          toast.success('Đã xóa phiếu nháp')
          navigate('/kho/phieu', { replace: true })
        },
      })
    },
  }
}
