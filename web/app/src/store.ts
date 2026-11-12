import { configureStore, isPlain } from '@reduxjs/toolkit';
import { baseApi } from '@/api/baseApi';

export const store = configureStore({
  reducer: {
    [baseApi.reducerPath]: baseApi.reducer,
  },
  middleware: (getDefaultMiddleware) =>
    // A downloaded file is cached as the Blob the API answered; everything else must still be plain.
    getDefaultMiddleware({
      serializableCheck: { isSerializable: (value: unknown) => isPlain(value) || value instanceof Blob },
    }).concat(baseApi.middleware),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
