import apiClient from './client';
import type { LabResultResponse, LabResultDetailResponse } from '../types/labResults';

export const getPatientLabResults = (patientId: string) =>
  apiClient.get<LabResultResponse[]>(`/lab-results/patient/${patientId}`).then((r) => r.data);

export const getLabResultById = (id: string) =>
  apiClient.get<LabResultDetailResponse>(`/lab-results/${id}`).then((r) => r.data);

export const getLabResultByAccession = (accessionNumber: string) =>
  apiClient.get<LabResultDetailResponse>(`/lab-results/order/${accessionNumber}`).then((r) => r.data);

export const simulateHl7Message = (rawHl7: string) => {
  const segments = rawHl7.split(/[\r\n]+/);
  const pid = segments.find(s => s.startsWith('PID|'))?.split('|') ?? [];
  const obr = segments.find(s => s.startsWith('OBR|'))?.split('|') ?? [];
  const obxs = segments.filter(s => s.startsWith('OBX|')).map(s => s.split('|'));

  const patientMrn = pid[3]?.split('^')[0] || '';
  const patientName = pid[5]?.replace(/\^/g, ' ') || '';
  const accessionNumber = obr[3]?.split('^')[0] || '';
  const doctorId = obr[16]?.split('^')[0] || '';

  const observations = obxs.map(f => {
    const obsId = f[3] ?? '';
    const parts = obsId.split('^');
    const testCode = parts[0] || obsId;
    const testName = parts[1] || testCode;
    const value = f[5] || '';
    const unit = f[6]?.split('^')[0] || '';
    const range = f[7] || '';
    const flag = f[8] || '';

    return {
      testCode,
      testName,
      value,
      unit,
      referenceRange: range,
      abnormalFlag: flag
    };
  });

  return apiClient.post('/dev/hl7/send', {
    accessionNumber,
    patientMrn,
    patientName,
    doctorId,
    observations
  }).then(r => r.data);
};
