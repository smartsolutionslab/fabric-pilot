import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { type FieldPath, useForm } from 'react-hook-form';
import { Link } from 'react-router';
import { useGetTenantsQuery, usePostContractsContractsByTenantMutation } from '@/api/generated';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { NativeSelect } from '@/components/ui/native-select';
import { type RecordContractForm, recordContractSchema } from './recordContractSchema';

/** The field each refusal of the API is about; anything else is shown above the form. */
const fieldOf: Record<string, FieldPath<RecordContractForm>> = {
  'tenant-not-found': 'tenant',
  'contracts.invalid-product': 'product',
  'contracts.ends-before-it-starts': 'endsOn',
};

interface Problem {
  data?: { code?: string; title?: string };
}

const empty: RecordContractForm = { tenant: '', product: '', startsOn: '', endsOn: '', renews: true };

/** Staff record a contract for a customer tenant, chosen from the tenants the host serves. */
export function RecordContractPage() {
  // The host lists at most 100 tenants a page; the pilot serves far fewer.
  const { data: tenants, isError } = useGetTenantsQuery({ page: 1, pageSize: 100 });
  const [record, { isLoading: isRecording }] = usePostContractsContractsByTenantMutation();
  const [recorded, setRecorded] = useState<string>();
  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors },
  } = useForm({ resolver: zodResolver(recordContractSchema), defaultValues: empty });

  const onSubmit = handleSubmit(async ({ tenant, ...contract }) => {
    setRecorded(undefined);
    try {
      await record({ tenant, recordContract: contract }).unwrap();
      setRecorded(`Recorded ${contract.product} for ${tenant}.`);
      // The next contract is often the same tenant's.
      reset({ ...empty, tenant });
    } catch (failure) {
      const { code, title } = (failure as Problem).data ?? {};
      setError(fieldOf[code ?? ''] ?? 'root', { message: title ?? 'The contract could not be recorded.' });
    }
  });

  const describedBy = (field: FieldPath<RecordContractForm>) => (errors[field] ? `${field}-error` : undefined);
  const fieldError = (field: FieldPath<RecordContractForm>) =>
    errors[field] ? (
      <p id={`${field}-error`} role="alert" className="text-sm text-destructive">
        {errors[field].message}
      </p>
    ) : null;

  return (
    <main className="mx-auto flex max-w-3xl flex-col gap-4 p-4">
      <Link to="/contracts">Back to the contracts</Link>
      <h1 className="text-2xl font-semibold">Record a contract</h1>
      {isError ? <p role="alert">The tenants could not be loaded.</p> : null}
      {errors.root ? (
        <p role="alert" className="text-sm text-destructive">
          {errors.root.message}
        </p>
      ) : null}
      {recorded ? <p role="status">{recorded}</p> : null}
      <form noValidate onSubmit={(event) => void onSubmit(event)} className="flex flex-col gap-4">
        <div className="flex flex-col gap-2">
          <Label htmlFor="tenant">Tenant</Label>
          <NativeSelect
            id="tenant"
            aria-invalid={Boolean(errors.tenant)}
            aria-describedby={describedBy('tenant')}
            {...register('tenant')}
          >
            <option value="">Choose a tenant…</option>
            {tenants?.items.map((tenant) => (
              <option key={tenant} value={tenant}>
                {tenant}
              </option>
            ))}
          </NativeSelect>
          {fieldError('tenant')}
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="product">Product</Label>
          <Input
            id="product"
            aria-invalid={Boolean(errors.product)}
            aria-describedby={describedBy('product')}
            {...register('product')}
          />
          {fieldError('product')}
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="startsOn">First day</Label>
          <Input
            id="startsOn"
            type="date"
            aria-invalid={Boolean(errors.startsOn)}
            aria-describedby={describedBy('startsOn')}
            {...register('startsOn')}
          />
          {fieldError('startsOn')}
        </div>
        <div className="flex flex-col gap-2">
          <Label htmlFor="endsOn">Last day</Label>
          <Input
            id="endsOn"
            type="date"
            aria-invalid={Boolean(errors.endsOn)}
            aria-describedby={describedBy('endsOn')}
            {...register('endsOn')}
          />
          {fieldError('endsOn')}
        </div>
        <div className="flex items-center gap-2">
          <input id="renews" type="checkbox" className="size-4" {...register('renews')} />
          <Label htmlFor="renews">Renews after its last day</Label>
        </div>
        <div>
          <Button type="submit" disabled={isRecording}>
            Record
          </Button>
        </div>
      </form>
    </main>
  );
}
