export interface FacilitySettingsResponse {
  facilitySettingsId: string;
  facilityName: string;
  address: string | null;
  phone: string | null;
  email: string | null;
  logoUrl: string | null;
  hasLogo: boolean;
  updatedAt: string;
}

export interface SaveFacilitySettingsRequest {
  facilityName: string;
  address?: string;
  phone?: string;
  email?: string;
}
