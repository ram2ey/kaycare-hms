using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KayCare.Core.Entities;
using KayCare.Core.Interfaces;
using KayCare.Infrastructure.Data;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<AiController> _logger;
    private static readonly HttpClient _httpClient = new();

    private static readonly string[] FreeModelsFallback = new[]
    {
        "meta-llama/llama-3.3-70b-instruct:free",
        "google/gemini-2.0-flash-lite-preview:free",
        "deepseek/deepseek-r1:free",
        "qwen/qwen-2.5-coder-32b-instruct:free"
    };

    private static readonly string[] ProModelsFallback = new[]
    {
        "openai/gpt-4o-mini",
        "google/gemini-2.0-flash",
        "meta-llama/llama-3.3-70b-instruct:free"
    };

    private static readonly string[] EnterpriseModelsFallback = new[]
    {
        "anthropic/claude-3.5-sonnet",
        "openai/gpt-4o",
        "deepseek/deepseek-r1"
    };

    private static readonly string[] FreeVisionModelsFallback = new[]
    {
        "google/gemini-2.0-flash-lite-preview:free",
        "meta-llama/llama-3.2-11b-vision-instruct:free"
    };

    private static readonly string[] ProVisionModelsFallback = new[]
    {
        "openai/gpt-4o-mini",
        "google/gemini-2.0-flash"
    };

    public AiController(IConfiguration config, AppDbContext db, ITenantContext tenantContext, ILogger<AiController> logger)
    {
        _config = config;
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    [HttpPost("soap-copilot")]
    public async Task<IActionResult> SoapCopilot([FromBody] SoapRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text is required." });
        }

        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        string prompt = $@"You are an AI Clinical Assistant. You are given a doctor's raw, unstructured consultation notes:
""{request.Text}""

Organize them into a structured JSON object with the following keys:
- subjective (detailed patient symptoms, complaints, duration, history)
- objective (vitals, physical exams, clinical observations)
- assessment (clinical impression, suspected diagnoses)
- plan (medications, tests, recommendations, lifestyle guidance, follow-up)
- primaryCode (suggested ICD-10 primary diagnosis code, e.g. J02.9)
- primaryDesc (description matching the primary code)
- secondary (an array of secondary diagnoses objects, each with 'code' and 'description' keys, or empty if none)

Provide ONLY a raw JSON response matching this schema without any markdown formatting. Do not wrap it in ```json.";

        string? result = await CallOpenRouterAsync(prompt, tenant, jsonMode: true);

        if (result != null)
        {
            try
            {
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(cleaned.IndexOf('\n')).Trim();
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
                }
                using var doc = JsonDocument.Parse(cleaned);
                await TrackAiUsageAsync(tenant, ct);
                return Content(cleaned, "application/json");
            }
            catch
            {
                // Fall through to 503
            }
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("patient-summary")]
    public async Task<IActionResult> PatientSummary([FromBody] SummaryRequest request, CancellationToken ct)
    {
        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        string prompt = $@"You are a supportive clinical assistant. Translate the following medical SOAP notes and plan into a warm, patient-friendly, clear set of take-home instructions in plain English:
SOAP Notes:
- Subjective: {request.Subjective}
- Objective: {request.Objective}
- Assessment: {request.Assessment}
- Plan: {request.Plan}

Include:
1. A summary of what we found in plain words.
2. Clear instructions on what they need to do (medications, self-care).
3. Red flags (when to seek emergency care).

Format the output clearly as a patient leaflet.";

        string? result = await CallOpenRouterAsync(prompt, tenant, jsonMode: false);
        if (result != null)
        {
            await TrackAiUsageAsync(tenant, ct);
            return Ok(new { summary = result });
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("lab-interpreter")]
    public async Task<IActionResult> LabInterpreter([FromBody] LabInterpreterRequest request, CancellationToken ct)
    {
        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        // M7 (partial de-identification): the patient's real name never leaves this server —
        // the model only ever sees PatientPlaceholder, and the real name is substituted back
        // into the response afterward so the interpretation still reads naturally.
        string resultsJson = JsonSerializer.Serialize(request.Results);
        string prompt = $@"You are an expert Clinical Pathologist. Review the following laboratory results for this patient and provide a concise, structured interpretation for the requesting doctor.
Patient Name: {PatientPlaceholder}
Test Panel: {request.TestName}

Results:
{resultsJson}

Format your response in Markdown with these sections:
1. **Clinical Assessment Summary**: Overview of the findings (e.g. anemia, hyperglycemia).
2. **Abnormal / Critical Flags**: Detail each abnormal value, what it indicates, and severity.
3. **Pathophysiological Correlations**: What potential underlying conditions explain these values.
4. **Recommended Next Steps**: Recommended follow-up tests, monitoring, or clinical interventions.
5. **Medical Disclaimer**: Standard AI clinical guidance disclaimer.

Refer to the patient only as {PatientPlaceholder} throughout your response — do not use any other name.
Be concise, technical, and professional.";

        string? result = await CallOpenRouterAsync(prompt, tenant, jsonMode: false);
        if (result != null)
        {
            result = Reidentify(result, (PatientPlaceholder, request.PatientName));
            await TrackAiUsageAsync(tenant, ct);
            return Ok(new { interpretation = result });
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("drug-safety")]
    public async Task<IActionResult> DrugSafety([FromBody] DrugSafetyRequest request, CancellationToken ct)
    {
        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        string drugsJson = JsonSerializer.Serialize(request.Items);
        string prompt = $@"You are a Clinical Pharmacist. Review the following medication cart for potential safety risks, drug-drug interactions, and controlled substance flags.
Medications:
{drugsJson}

Provide a structured response in Markdown containing:
1. **Drug-Drug Interactions**: High, moderate, or minor interactions between the listed items.
2. **Controlled Substance Alerts**: Highlight any controlled substances and compliance requirements.
3. **Patient Counseling Guidelines**: Key points for patient counseling (e.g. food requirements, warnings, alcohol interactions).

Be professional, concise, and focused on patient safety.";

        string? result = await CallOpenRouterAsync(prompt, tenant, jsonMode: false);
        if (result != null)
        {
            await TrackAiUsageAsync(tenant, ct);
            return Ok(new { interactions = result });
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("prescription-ocr")]
    public async Task<IActionResult> PrescriptionOcr([FromBody] PrescriptionOcrRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Base64Image))
        {
            return BadRequest(new { error = "Image is required." });
        }

        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        string prompt = @"You are a Clinical Pharmacist and Medical Document Parser. Analyze this prescription image or handwritten doctor note.
Extract all prescribed medications into a JSON array of objects with the following schema:
- drugName: Name of the medication (brand or generic)
- dosage: Strength/dosage (e.g. 500mg, 10ml)
- frequency: How often to take (e.g. Once daily, Twice daily, Every 8 hours)
- duration: Duration of treatment (e.g. 7 days, 1 month)
- quantity: Estimated numeric quantity to dispense (integer)
- instructions: Special instructions (e.g. Take after meals)

Return ONLY a raw JSON array matching this schema without any markdown backticks or explanation text.";

        string? result = await CallOpenRouterMultimodalAsync(prompt, request.Base64Image, request.MimeType ?? "image/jpeg", tenant);

        if (result != null)
        {
            try
            {
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(cleaned.IndexOf('\n')).Trim();
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
                }
                using var doc = JsonDocument.Parse(cleaned);
                await TrackAiUsageAsync(tenant, ct);
                return Content(cleaned, "application/json");
            }
            catch
            {
                // Fall through
            }
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("discharge-summary")]
    public async Task<IActionResult> DischargeSummary([FromBody] DischargeSummaryRequest request, CancellationToken ct)
    {
        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        // M7 (partial de-identification): real name/MRN never leave this server — see LabInterpreter.
        string prompt = $@"You are a Chief Medical Officer drafting a formal Hospital Discharge Summary & Transfer Note.
Patient Name: {PatientPlaceholder} | Age/Gender: {request.AgeGender} | MRN: {MrnPlaceholder}
Admission Date: {request.AdmissionDate} | Discharge Date: {request.DischargeDate}

Hospitalization Context:
- Admission Reason & History: {request.AdmissionReason}
- Consultation & Clinical Notes: {request.ConsultationNotes}
- Lab & Investigation Results: {request.LabResults}
- Inpatient Procedures / Treatment: {request.TreatmentGiven}
- Discharge Medications: {request.DischargeMedications}

Generate a comprehensive Markdown report structured with these exact headers:
# Hospital Discharge Summary
### 1. Patient & Admission Overview
### 2. Clinical History & Reason for Admission
### 3. Summary of Inpatient Course & Investigations
### 4. Final Diagnoses
### 5. Discharge Medications & Dosage Schedule
### 6. Post-Discharge Care & Red Flag Warning Signs
### 7. Follow-Up Appointments & Referrals

Refer to the patient only as {PatientPlaceholder} and their record number only as {MrnPlaceholder} throughout — do not use any other identifying name or number.
Be professional, thorough, and precise.";

        string? result = await CallOpenRouterAsync(prompt, tenant, jsonMode: false);
        if (result != null)
        {
            result = Reidentify(result, (PatientPlaceholder, request.PatientName), (MrnPlaceholder, request.Mrn));
            await TrackAiUsageAsync(tenant, ct);
            return Ok(new { summary = result });
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("insurance-appeal")]
    public async Task<IActionResult> InsuranceAppeal([FromBody] InsuranceAppealRequest request, CancellationToken ct)
    {
        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        // M7 (partial de-identification): real name never leaves this server — see LabInterpreter.
        // ClaimNumber/PayerName are billing/administrative references, not patient identifiers, and
        // the letter needs the real claim number to be a usable document — left as-is.
        string prompt = $@"You are a Medical Billing Specialist & Physician Appeals Officer. Write a formal, convincing Insurance Clinical Pre-Authorization & Claim Appeal Letter for this patient.
Patient Name: {PatientPlaceholder} | Claim / Pre-Auth Number: {request.ClaimNumber}
Insurer: {request.PayerName} | Total Amount: {request.TotalAmount}
Primary Diagnosis: {request.PrimaryDiagnosis} (ICD-10: {request.IcdCode})
Service / Procedure Billed: {request.ProcedureName}
Reason Billed / Medical Necessity Notes: {request.MedicalNecessityNotes}
Rejection Reason (if appeal): {request.RejectionReason}

Write a complete, formal appeal letter in Markdown addressed to the Medical Director of the insurance company.
Refer to the patient only as {PatientPlaceholder} throughout the letter — do not use any other name.
Include clinical justifications, medical necessity guidelines, ICD-10 correlation, and formal request for approval/reimbursement.";

        string? result = await CallOpenRouterAsync(prompt, tenant, jsonMode: false);
        if (result != null)
        {
            result = Reidentify(result, (PatientPlaceholder, request.PatientName));
            await TrackAiUsageAsync(tenant, ct);
            return Ok(new { letter = result });
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("triage-score")]
    public async Task<IActionResult> TriageScore([FromBody] TriageScoreRequest request, CancellationToken ct)
    {
        var (allowed, errorResult, tenant) = await ValidateAiAccessAsync(ct);
        if (!allowed) return errorResult!;

        // M7 (partial de-identification): real name never leaves this server — see LabInterpreter.
        string prompt = $@"You are an Emergency Triage Nurse & Acute Care Specialist. Evaluate the following emergency patient intake:
Patient: {PatientPlaceholder} | Age: {request.Age}
Chief Complaint: ""{request.ChiefComplaint}""
Vitals:
- Heart Rate: {request.HeartRate} bpm
- Blood Pressure: {request.BloodPressure}
- Temperature: {request.Temperature} °C
- SpO2: {request.SpO2} %
- Resp Rate: {request.RespiratoryRate} /min

Evaluate the patient and return a JSON object with:
- esiScore: Integer (1 for Resuscitation/Immediate, 2 for Emergent, 3 for Urgent, 4 for Less Urgent, 5 for Non-Urgent)
- category: ESI Category name (e.g. ""ESI Level 2 - Emergent"")
- priorityLevel: High, Medium, or Low
- rationale: Explanation of the score based on vitals and symptoms (refer to the patient only as ""{PatientPlaceholder}"", never by any other name)
- immediateActions: List of string nursing/medical actions to take immediately
- redFlags: List of warning signs to monitor continuously

Return ONLY a raw JSON object matching this schema without markdown backticks.";

        string? result = await CallOpenRouterAsync(prompt, tenant, jsonMode: true);
        if (result != null)
        {
            result = Reidentify(result, (PatientPlaceholder, request.PatientName));
            try
            {
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(cleaned.IndexOf('\n')).Trim();
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
                }
                using var doc = JsonDocument.Parse(cleaned);
                await TrackAiUsageAsync(tenant, ct);
                return Content(cleaned, "application/json");
            }
            catch
            {
                // Fall through
            }
        }

        return StatusCode(503, new { error = "AI Assistant is temporarily unavailable." });
    }

    // M7 (partial de-identification): structured identifier fields (patient name, MRN) are
    // interpolated into prompts as one of these placeholders instead of their real value, and
    // Reidentify() substitutes the real value back into the model's response afterward — the
    // third-party AI provider never sees the real identifier, but the document returned to the
    // clinician still reads naturally. This only covers structured fields; free-text clinical
    // narrative (SOAP notes, consultation notes, etc.) may still incidentally mention identifying
    // details inline, and PrescriptionOcr's image payload isn't covered at all — both would need
    // NLP/image redaction respectively, out of scope here. Endpoints with no structured identifier
    // field in their request (SoapCopilot, PatientSummary, DrugSafety) have nothing to redact.
    private const string PatientPlaceholder = "[PATIENT]";
    private const string MrnPlaceholder     = "[MRN]";

    private static string Reidentify(string text, params (string Placeholder, string? Value)[] identifiers)
    {
        foreach (var (placeholder, value) in identifiers)
        {
            if (string.IsNullOrWhiteSpace(value)) continue;
            text = text.Replace(placeholder, value, StringComparison.OrdinalIgnoreCase);
        }
        return text;
    }

    private async Task<(bool Allowed, IActionResult? ErrorResult, Tenant? Tenant)> ValidateAiAccessAsync(CancellationToken ct)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty) return (true, null, null);

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.TenantId == tenantId, ct);
        if (tenant == null) return (true, null, null);

        // 1. Lock Check (Tier 0: No AI / Opt-Out)
        if (!tenant.IsAiEnabled)
        {
            return (false, StatusCode(403, new { error = "AI Assistant features are disabled / opted-out for this facility. Contact your administrator to enable AI." }), tenant);
        }

        // 2. Monthly Quota Check
        if (tenant.AiRequestsThisMonth >= tenant.AiMonthlyQuota)
        {
            return (false, StatusCode(429, new { error = $"Monthly AI Assistant quota ({tenant.AiMonthlyQuota} requests) reached for this facility. Please contact your administrator to upgrade your plan." }), tenant);
        }

        return (true, null, tenant);
    }

    private async Task TrackAiUsageAsync(Tenant? tenant, CancellationToken ct)
    {
        if (tenant == null) return;
        try
        {
            tenant.AiRequestsThisMonth += 1;
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Non-blocking usage counter update
        }
    }

    // L5 — single shared resolution logic (was duplicated verbatim in both Call* methods below).
    // Dropped the old Gemini:ApiKey/GEMINI_API_KEY fallback entirely: a Gemini key is not a valid
    // OpenRouter bearer token, so that branch could never actually authenticate a request — it
    // only turned a clear "no key configured" signal into an opaque generic 503.
    private string? ResolveApiKey(Tenant? tenant)
    {
        if (!string.IsNullOrWhiteSpace(tenant?.CustomOpenRouterKey))
            return tenant.CustomOpenRouterKey;

        var key = _config["OpenRouter:ApiKey"]
               ?? _config["OPENROUTER_API_KEY"]
               ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

        if (string.IsNullOrEmpty(key))
        {
            _logger.LogWarning(
                "No OpenRouter API key configured for tenant {TenantId} — no tenant BYOK key, and OpenRouter:ApiKey/OPENROUTER_API_KEY are both unset.",
                tenant?.TenantId);
        }

        return key;
    }

    private string[] ResolveModels(Tenant? tenant, string[] freeModels, string[] proModels, string[] enterpriseModels)
    {
        var tier = tenant?.AllowedAiTiers ?? tenant?.SubscriptionPlan ?? "Standard";

        var modelsToUse = tier.Equals("Enterprise", StringComparison.OrdinalIgnoreCase) ? enterpriseModels
            : tier.Equals("Pro", StringComparison.OrdinalIgnoreCase) ? proModels
            : freeModels;

        var customModel = _config["OpenRouter:Model"]
                       ?? _config["OPENROUTER_MODEL"]
                       ?? Environment.GetEnvironmentVariable("OPENROUTER_MODEL");

        if (!string.IsNullOrWhiteSpace(customModel) && customModel != "openrouter/auto")
        {
            modelsToUse = new[] { customModel };
        }

        return modelsToUse;
    }

    private async Task<string?> CallOpenRouterAsync(string prompt, Tenant? tenant, bool jsonMode = false)
    {
        var apiKey = ResolveApiKey(tenant);
        if (string.IsNullOrEmpty(apiKey)) return null;

        var modelsToUse = ResolveModels(tenant, FreeModelsFallback, ProModelsFallback, EnterpriseModelsFallback);

        object requestBody = jsonMode ? new
        {
            models = modelsToUse,
            messages = new[] { new { role = "user", content = prompt } },
            response_format = new { type = "json_object" }
        } : new
        {
            models = modelsToUse,
            messages = new[] { new { role = "user", content = prompt } }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Headers.Add("HTTP-Referer", "https://kaycare.com");
            req.Headers.Add("X-Title", "KayCare HMS");
            req.Content = jsonContent;

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode) return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }
        }
        catch
        {
            // Fail silent
        }

        return null;
    }

    private async Task<string?> CallOpenRouterMultimodalAsync(string prompt, string base64Data, string mimeType, Tenant? tenant)
    {
        var apiKey = ResolveApiKey(tenant);
        if (string.IsNullOrEmpty(apiKey)) return null;

        var imageUrl = base64Data.StartsWith("data:") ? base64Data : $"data:{mimeType};base64,{base64Data}";

        // Vision has only two tiers (no separate Enterprise model list) — passing the same array
        // for both the pro and enterprise slots reproduces the original Enterprise-or-Pro grouping.
        var modelsToUse = ResolveModels(tenant, FreeVisionModelsFallback, ProVisionModelsFallback, ProVisionModelsFallback);

        var requestBody = new
        {
            models = modelsToUse,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = imageUrl }
                        }
                    }
                }
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Headers.Add("HTTP-Referer", "https://kaycare.com");
            req.Headers.Add("X-Title", "KayCare HMS");
            req.Content = jsonContent;

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode) return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }
        }
        catch
        {
            // Fail silent
        }

        return null;
    }
}

public class SoapRequest
{
    public string Text { get; set; } = string.Empty;
}

public class SummaryRequest
{
    public string Subjective { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;
}

public class LabInterpreterRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public List<LabResultItem> Results { get; set; } = [];
}

public class LabResultItem
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string RefRange { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
}

public class DrugSafetyRequest
{
    public List<DrugSafetyItem> Items { get; set; } = [];
}

public class DrugSafetyItem
{
    public string DrugName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string Dosage { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class PrescriptionOcrRequest
{
    public string Base64Image { get; set; } = string.Empty;
    public string? MimeType { get; set; }
}

public class DischargeSummaryRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string AgeGender { get; set; } = string.Empty;
    public string Mrn { get; set; } = string.Empty;
    public string AdmissionDate { get; set; } = string.Empty;
    public string DischargeDate { get; set; } = string.Empty;
    public string AdmissionReason { get; set; } = string.Empty;
    public string ConsultationNotes { get; set; } = string.Empty;
    public string LabResults { get; set; } = string.Empty;
    public string TreatmentGiven { get; set; } = string.Empty;
    public string DischargeMedications { get; set; } = string.Empty;
}

public class InsuranceAppealRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public string PrimaryDiagnosis { get; set; } = string.Empty;
    public string IcdCode { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string MedicalNecessityNotes { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
}

public class TriageScoreRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string Age { get; set; } = string.Empty;
    public string ChiefComplaint { get; set; } = string.Empty;
    public int HeartRate { get; set; }
    public string BloodPressure { get; set; } = string.Empty;
    public double Temperature { get; set; }
    public double SpO2 { get; set; }
    public int RespiratoryRate { get; set; }
}
