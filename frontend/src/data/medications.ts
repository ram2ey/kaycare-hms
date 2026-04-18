export interface MedicationEntry {
  name: string;
  genericName: string;
  strengths: string[];
  dosageForm: string;
  isControlledSubstance?: boolean;
}

export const MEDICATION_CATALOG: MedicationEntry[] = [
  // ── Analgesics ────────────────────────────────────────────────────────────
  { name: 'Paracetamol', genericName: 'Acetaminophen', strengths: ['500mg', '1000mg', '250mg/5ml'], dosageForm: 'Tablet' },
  { name: 'Ibuprofen', genericName: 'Ibuprofen', strengths: ['200mg', '400mg', '600mg', '100mg/5ml'], dosageForm: 'Tablet' },
  { name: 'Diclofenac', genericName: 'Diclofenac sodium', strengths: ['25mg', '50mg', '75mg', '100mg'], dosageForm: 'Tablet' },
  { name: 'Tramadol', genericName: 'Tramadol hydrochloride', strengths: ['50mg', '100mg'], dosageForm: 'Capsule', isControlledSubstance: true },
  { name: 'Codeine', genericName: 'Codeine phosphate', strengths: ['15mg', '30mg', '60mg'], dosageForm: 'Tablet', isControlledSubstance: true },
  { name: 'Morphine', genericName: 'Morphine sulfate', strengths: ['10mg', '15mg', '30mg'], dosageForm: 'Tablet', isControlledSubstance: true },
  { name: 'Aspirin', genericName: 'Acetylsalicylic acid', strengths: ['75mg', '150mg', '300mg', '500mg'], dosageForm: 'Tablet' },

  // ── Antibiotics ───────────────────────────────────────────────────────────
  { name: 'Amoxicillin', genericName: 'Amoxicillin', strengths: ['250mg', '500mg', '125mg/5ml'], dosageForm: 'Capsule' },
  { name: 'Co-amoxiclav', genericName: 'Amoxicillin/Clavulanate', strengths: ['375mg', '625mg', '1g'], dosageForm: 'Tablet' },
  { name: 'Ciprofloxacin', genericName: 'Ciprofloxacin', strengths: ['250mg', '500mg', '750mg'], dosageForm: 'Tablet' },
  { name: 'Metronidazole', genericName: 'Metronidazole', strengths: ['200mg', '400mg', '500mg'], dosageForm: 'Tablet' },
  { name: 'Azithromycin', genericName: 'Azithromycin', strengths: ['250mg', '500mg'], dosageForm: 'Tablet' },
  { name: 'Doxycycline', genericName: 'Doxycycline hyclate', strengths: ['100mg'], dosageForm: 'Capsule' },
  { name: 'Erythromycin', genericName: 'Erythromycin', strengths: ['250mg', '500mg'], dosageForm: 'Tablet' },
  { name: 'Clindamycin', genericName: 'Clindamycin hydrochloride', strengths: ['150mg', '300mg'], dosageForm: 'Capsule' },
  { name: 'Ceftriaxone', genericName: 'Ceftriaxone sodium', strengths: ['500mg', '1g', '2g'], dosageForm: 'Injection' },
  { name: 'Ampicillin', genericName: 'Ampicillin', strengths: ['250mg', '500mg'], dosageForm: 'Capsule' },
  { name: 'Trimethoprim-Sulfamethoxazole', genericName: 'Co-trimoxazole', strengths: ['480mg', '960mg'], dosageForm: 'Tablet' },
  { name: 'Nitrofurantoin', genericName: 'Nitrofurantoin', strengths: ['50mg', '100mg'], dosageForm: 'Capsule' },
  { name: 'Gentamicin', genericName: 'Gentamicin sulfate', strengths: ['40mg/ml', '80mg/2ml'], dosageForm: 'Injection' },

  // ── Antimalarials ─────────────────────────────────────────────────────────
  { name: 'Artemether-Lumefantrine', genericName: 'Artemether/Lumefantrine', strengths: ['20mg/120mg'], dosageForm: 'Tablet' },
  { name: 'Artesunate', genericName: 'Artesunate', strengths: ['50mg', '100mg', '200mg'], dosageForm: 'Tablet' },
  { name: 'Quinine', genericName: 'Quinine sulfate', strengths: ['300mg', '600mg'], dosageForm: 'Tablet' },
  { name: 'Chloroquine', genericName: 'Chloroquine phosphate', strengths: ['150mg', '250mg'], dosageForm: 'Tablet' },
  { name: 'Primaquine', genericName: 'Primaquine phosphate', strengths: ['7.5mg', '15mg'], dosageForm: 'Tablet' },

  // ── Antihypertensives ─────────────────────────────────────────────────────
  { name: 'Amlodipine', genericName: 'Amlodipine besylate', strengths: ['5mg', '10mg'], dosageForm: 'Tablet' },
  { name: 'Lisinopril', genericName: 'Lisinopril', strengths: ['2.5mg', '5mg', '10mg', '20mg'], dosageForm: 'Tablet' },
  { name: 'Losartan', genericName: 'Losartan potassium', strengths: ['25mg', '50mg', '100mg'], dosageForm: 'Tablet' },
  { name: 'Atenolol', genericName: 'Atenolol', strengths: ['25mg', '50mg', '100mg'], dosageForm: 'Tablet' },
  { name: 'Nifedipine', genericName: 'Nifedipine', strengths: ['10mg', '20mg', '30mg'], dosageForm: 'Tablet' },
  { name: 'Hydrochlorothiazide', genericName: 'Hydrochlorothiazide', strengths: ['12.5mg', '25mg', '50mg'], dosageForm: 'Tablet' },
  { name: 'Methyldopa', genericName: 'Methyldopa', strengths: ['250mg', '500mg'], dosageForm: 'Tablet' },
  { name: 'Furosemide', genericName: 'Furosemide', strengths: ['20mg', '40mg', '80mg'], dosageForm: 'Tablet' },
  { name: 'Spironolactone', genericName: 'Spironolactone', strengths: ['25mg', '50mg', '100mg'], dosageForm: 'Tablet' },
  { name: 'Carvedilol', genericName: 'Carvedilol', strengths: ['3.125mg', '6.25mg', '12.5mg', '25mg'], dosageForm: 'Tablet' },

  // ── Diabetes ──────────────────────────────────────────────────────────────
  { name: 'Metformin', genericName: 'Metformin hydrochloride', strengths: ['500mg', '850mg', '1000mg'], dosageForm: 'Tablet' },
  { name: 'Glibenclamide', genericName: 'Glibenclamide', strengths: ['2.5mg', '5mg'], dosageForm: 'Tablet' },
  { name: 'Glipizide', genericName: 'Glipizide', strengths: ['2.5mg', '5mg', '10mg'], dosageForm: 'Tablet' },
  { name: 'Insulin Regular', genericName: 'Insulin (human, soluble)', strengths: ['100 IU/ml'], dosageForm: 'Injection' },
  { name: 'Insulin NPH', genericName: 'Isophane insulin', strengths: ['100 IU/ml'], dosageForm: 'Injection' },
  { name: 'Sitagliptin', genericName: 'Sitagliptin phosphate', strengths: ['50mg', '100mg'], dosageForm: 'Tablet' },

  // ── Gastrointestinal ──────────────────────────────────────────────────────
  { name: 'Omeprazole', genericName: 'Omeprazole', strengths: ['10mg', '20mg', '40mg'], dosageForm: 'Capsule' },
  { name: 'Ranitidine', genericName: 'Ranitidine hydrochloride', strengths: ['150mg', '300mg'], dosageForm: 'Tablet' },
  { name: 'Metoclopramide', genericName: 'Metoclopramide hydrochloride', strengths: ['10mg', '5mg/5ml'], dosageForm: 'Tablet' },
  { name: 'Domperidone', genericName: 'Domperidone', strengths: ['10mg', '5mg/5ml'], dosageForm: 'Tablet' },
  { name: 'Loperamide', genericName: 'Loperamide hydrochloride', strengths: ['2mg'], dosageForm: 'Capsule' },
  { name: 'Bisacodyl', genericName: 'Bisacodyl', strengths: ['5mg', '10mg'], dosageForm: 'Tablet' },
  { name: 'Oral Rehydration Salts', genericName: 'ORS', strengths: ['20.5g/sachet'], dosageForm: 'Other' },

  // ── Respiratory ───────────────────────────────────────────────────────────
  { name: 'Salbutamol', genericName: 'Albuterol', strengths: ['2mg', '4mg', '100mcg/dose'], dosageForm: 'Tablet' },
  { name: 'Prednisolone', genericName: 'Prednisolone', strengths: ['5mg', '10mg', '20mg', '25mg'], dosageForm: 'Tablet' },
  { name: 'Beclomethasone', genericName: 'Beclomethasone dipropionate', strengths: ['50mcg/dose', '100mcg/dose'], dosageForm: 'Inhaler' },
  { name: 'Aminophylline', genericName: 'Aminophylline', strengths: ['100mg', '200mg'], dosageForm: 'Tablet' },
  { name: 'Cetirizine', genericName: 'Cetirizine hydrochloride', strengths: ['5mg', '10mg'], dosageForm: 'Tablet' },
  { name: 'Loratadine', genericName: 'Loratadine', strengths: ['10mg'], dosageForm: 'Tablet' },
  { name: 'Promethazine', genericName: 'Promethazine hydrochloride', strengths: ['10mg', '25mg'], dosageForm: 'Tablet' },

  // ── Cardiovascular ────────────────────────────────────────────────────────
  { name: 'Atorvastatin', genericName: 'Atorvastatin calcium', strengths: ['10mg', '20mg', '40mg', '80mg'], dosageForm: 'Tablet' },
  { name: 'Simvastatin', genericName: 'Simvastatin', strengths: ['10mg', '20mg', '40mg'], dosageForm: 'Tablet' },
  { name: 'Warfarin', genericName: 'Warfarin sodium', strengths: ['1mg', '2mg', '5mg'], dosageForm: 'Tablet', isControlledSubstance: false },
  { name: 'Clopidogrel', genericName: 'Clopidogrel bisulfate', strengths: ['75mg'], dosageForm: 'Tablet' },
  { name: 'Digoxin', genericName: 'Digoxin', strengths: ['62.5mcg', '125mcg', '250mcg'], dosageForm: 'Tablet' },

  // ── Antifungals ───────────────────────────────────────────────────────────
  { name: 'Fluconazole', genericName: 'Fluconazole', strengths: ['50mg', '150mg', '200mg'], dosageForm: 'Capsule' },
  { name: 'Clotrimazole', genericName: 'Clotrimazole', strengths: ['1%', '2%'], dosageForm: 'Cream' },
  { name: 'Nystatin', genericName: 'Nystatin', strengths: ['100,000 IU/ml', '500,000 IU'], dosageForm: 'Suspension' },
  { name: 'Griseofulvin', genericName: 'Griseofulvin', strengths: ['125mg', '500mg'], dosageForm: 'Tablet' },

  // ── Antiparasitics ────────────────────────────────────────────────────────
  { name: 'Albendazole', genericName: 'Albendazole', strengths: ['200mg', '400mg'], dosageForm: 'Tablet' },
  { name: 'Mebendazole', genericName: 'Mebendazole', strengths: ['100mg', '500mg'], dosageForm: 'Tablet' },
  { name: 'Ivermectin', genericName: 'Ivermectin', strengths: ['3mg', '6mg'], dosageForm: 'Tablet' },
  { name: 'Praziquantel', genericName: 'Praziquantel', strengths: ['600mg'], dosageForm: 'Tablet' },

  // ── Antiretrovirals ───────────────────────────────────────────────────────
  { name: 'Tenofovir', genericName: 'Tenofovir disoproxil fumarate', strengths: ['300mg'], dosageForm: 'Tablet' },
  { name: 'Lamivudine', genericName: 'Lamivudine', strengths: ['150mg', '300mg'], dosageForm: 'Tablet' },
  { name: 'Efavirenz', genericName: 'Efavirenz', strengths: ['200mg', '600mg'], dosageForm: 'Tablet' },
  { name: 'Lopinavir/Ritonavir', genericName: 'Lopinavir/Ritonavir', strengths: ['200mg/50mg'], dosageForm: 'Tablet' },
  { name: 'Nevirapine', genericName: 'Nevirapine', strengths: ['200mg', '400mg'], dosageForm: 'Tablet' },

  // ── Vitamins & Supplements ────────────────────────────────────────────────
  { name: 'Folic Acid', genericName: 'Folic acid', strengths: ['400mcg', '5mg'], dosageForm: 'Tablet' },
  { name: 'Ferrous Sulfate', genericName: 'Ferrous sulfate', strengths: ['200mg', '325mg'], dosageForm: 'Tablet' },
  { name: 'Vitamin C', genericName: 'Ascorbic acid', strengths: ['100mg', '200mg', '500mg'], dosageForm: 'Tablet' },
  { name: 'Vitamin B12', genericName: 'Cyanocobalamin', strengths: ['500mcg', '1000mcg'], dosageForm: 'Tablet' },
  { name: 'Calcium Carbonate', genericName: 'Calcium carbonate', strengths: ['500mg', '1000mg'], dosageForm: 'Tablet' },
  { name: 'Zinc Sulfate', genericName: 'Zinc sulfate', strengths: ['20mg', '45mg'], dosageForm: 'Tablet' },

  // ── Thyroid ───────────────────────────────────────────────────────────────
  { name: 'Levothyroxine', genericName: 'Levothyroxine sodium', strengths: ['25mcg', '50mcg', '100mcg'], dosageForm: 'Tablet' },
  { name: 'Carbimazole', genericName: 'Carbimazole', strengths: ['5mg', '10mg'], dosageForm: 'Tablet' },

  // ── Psychiatric ───────────────────────────────────────────────────────────
  { name: 'Amitriptyline', genericName: 'Amitriptyline hydrochloride', strengths: ['10mg', '25mg', '50mg'], dosageForm: 'Tablet' },
  { name: 'Haloperidol', genericName: 'Haloperidol', strengths: ['0.5mg', '1mg', '2mg', '5mg'], dosageForm: 'Tablet' },
  { name: 'Diazepam', genericName: 'Diazepam', strengths: ['2mg', '5mg', '10mg'], dosageForm: 'Tablet', isControlledSubstance: true },
  { name: 'Phenobarbitone', genericName: 'Phenobarbital', strengths: ['15mg', '30mg', '60mg', '100mg'], dosageForm: 'Tablet', isControlledSubstance: true },

  // ── Ophthalmology ─────────────────────────────────────────────────────────
  { name: 'Chloramphenicol Eye Drops', genericName: 'Chloramphenicol', strengths: ['0.5%'], dosageForm: 'Drops' },
  { name: 'Gentamicin Eye Drops', genericName: 'Gentamicin', strengths: ['0.3%'], dosageForm: 'Drops' },

  // ── Dermatology ───────────────────────────────────────────────────────────
  { name: 'Hydrocortisone Cream', genericName: 'Hydrocortisone', strengths: ['0.5%', '1%', '2.5%'], dosageForm: 'Cream' },
  { name: 'Betamethasone', genericName: 'Betamethasone valerate', strengths: ['0.05%', '0.1%'], dosageForm: 'Cream' },
  { name: 'Calamine Lotion', genericName: 'Calamine', strengths: ['15%'], dosageForm: 'Other' },
];

export function searchMedications(query: string): MedicationEntry[] {
  if (query.length < 2) return [];
  const q = query.toLowerCase();
  return MEDICATION_CATALOG.filter(
    (m) =>
      m.name.toLowerCase().includes(q) ||
      m.genericName.toLowerCase().includes(q)
  ).slice(0, 8);
}
