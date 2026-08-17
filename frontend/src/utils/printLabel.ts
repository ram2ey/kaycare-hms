export interface LabelFields {
  patientName: string;
  patientMrn: string;
  testName: string;
  department: string;
  accessionNumber: string | null;
}

/**
 * Opens a small popup and prints a lab specimen label. The HTML shell written via
 * document.write is entirely static — patient-controlled values (name, MRN, test name, etc.)
 * are never interpolated into markup. They're assigned via textContent after the document
 * loads, so nothing in them can execute as HTML/script even if a patient record contains
 * characters that look like markup.
 */
export function printLabel(fields: LabelFields) {
  const win = window.open('', '_blank', 'noopener,noreferrer,width=400,height=300');
  if (!win) return;

  win.document.write(`
    <html><head><title>Label</title>
    <style>
      body { font-family: monospace; margin: 10px; }
      .label { border: 1px dashed #333; padding: 8px; width: 280px; }
      h3 { margin: 0 0 4px; font-size: 13px; }
      p { margin: 2px 0; font-size: 11px; }
      .acc { font-size: 18px; font-weight: bold; letter-spacing: 3px; margin: 6px 0; }
    </style></head>
    <body>
      <div class="label">
        <h3 id="lbl-name"></h3>
        <p id="lbl-mrn"></p>
        <p id="lbl-test"></p>
        <p id="lbl-dept"></p>
        <div class="acc" id="lbl-acc-big"></div>
        <p id="lbl-acc"></p>
        <p id="lbl-date"></p>
      </div>
    </body></html>
  `);
  win.document.close();

  const setText = (id: string, text: string) => {
    const el = win.document.getElementById(id);
    if (el) el.textContent = text;
  };
  setText('lbl-name', fields.patientName);
  setText('lbl-mrn', `MRN: ${fields.patientMrn}`);
  setText('lbl-test', `Test: ${fields.testName}`);
  setText('lbl-dept', `Dept: ${fields.department}`);
  const accession = fields.accessionNumber ?? '';
  setText('lbl-acc-big', `|||  ${accession}  |||`);
  setText('lbl-acc', accession);
  setText('lbl-date', new Date().toLocaleDateString());

  win.print();
  win.close();
}
