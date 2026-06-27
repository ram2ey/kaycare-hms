export function safeArray<T>(data: T[] | null | undefined): T[];
export function safeArray<T>(data: { items?: T[] }): T[];
export function safeArray<T>(data: { data?: T[] }): T[];
export function safeArray<T>(data: { results?: T[] }): T[];
export function safeArray<T>(data: any): T[];
export function safeArray<T>(data: any): T[] {
  if (!data) return [];
  if (Array.isArray(data)) return data;
  if (Array.isArray(data.items)) return data.items;
  if (Array.isArray(data.data)) return data.data;
  if (Array.isArray(data.results)) return data.results;
  return [];
}
