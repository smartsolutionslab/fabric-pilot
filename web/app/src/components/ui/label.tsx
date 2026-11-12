import type { ComponentProps } from 'react';
import { cn } from '@/lib/utils';

/** shadcn/ui's label, written out by hand, on the plain element rather than Radix's. */
function Label({ className, ...props }: ComponentProps<'label'>) {
  return (
    <label
      data-slot="label"
      className={cn('flex items-center gap-2 text-sm leading-none font-medium select-none', className)}
      {...props}
    />
  );
}

export { Label };
