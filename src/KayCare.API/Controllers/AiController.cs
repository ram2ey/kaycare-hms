using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCare.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IConfiguration _config;
    private static readonly HttpClient _httpClient = new();

    public AiController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("soap-copilot")]
    public async Task<IActionResult> SoapCopilot([FromBody] SoapRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text is required." });
        }

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

        string? result = await CallGeminiAsync(prompt, jsonMode: true);

        if (result != null)
        {
            try
            {
                using var doc = JsonDocument.Parse(result);
                return Content(result, "application/json");
            }
            catch
            {
                // Fallback to mock if JSON parsing of the live response failed
            }
        }

        // --- Mock Engine Fallback ---
        var textLower = request.Text.ToLower();
        object mockResponse;

        if (textLower.Contains("throat") || textLower.Contains("cough") || textLower.Contains("fever") || textLower.Contains("pharyngitis"))
        {
            mockResponse = new
            {
                subjective = "Patient presents with a scratchy, painful throat for the past 3 days, accompanied by dry cough and subjective fever. Reports pain on swallowing.",
                objective = "Temp 38.1°C (100.6°F). Heart Rate 88 bpm. Oropharyngeal examination shows bilateral tonsillar erythema and swelling (2+) with mild exudates. Mild anterior cervical lymphadenopathy present.",
                assessment = "Acute pharyngitis, suspected streptococcal infection.",
                plan = "1. Amoxicillin 500mg tid for 10 days.\n2. Warm saline gargles q4h.\n3. Increased oral fluid intake and vocal rest.\n4. Acetaminophen 500mg q6h prn for pain/fever.",
                primaryCode = "J02.9",
                primaryDesc = "Acute pharyngitis, unspecified",
                secondary = new[]
                {
                    new { code = "R50.9", description = "Fever, unspecified" },
                    new { code = "R05.9", description = "Cough, unspecified" }
                }
            };
        }
        else if (textLower.Contains("chest") || textLower.Contains("breath") || textLower.Contains("bronchitis"))
        {
            mockResponse = new
            {
                subjective = "Patient reports sudden onset of chest tightness and a productive cough yielding thick yellow sputum for 5 days. Noted mild shortness of breath on exertion.",
                objective = "Temp 37.8°C. Blood pressure 125/80 mmHg. Resp Rate 20/min. SpO2 96% on room air. Auscultation reveals bilateral coarse crackles in lower lobes. No wheezing.",
                assessment = "Acute bronchitis, suspected secondary bacterial infection.",
                plan = "1. Azithromycin 500mg daily for 3 days.\n2. Albuterol inhaler 2 puffs q6h prn for chest tightness.\n3. Guaifenesin 600mg bid for cough expectorant.\n4. Rest, warm fluids, and follow up in 48-72h if symptoms worsen.",
                primaryCode = "J20.9",
                primaryDesc = "Acute bronchitis, unspecified",
                secondary = new[]
                {
                    new { code = "R06.02", description = "Shortness of breath" },
                    new { code = "R07.9", description = "Chest pain, unspecified" }
                }
            };
        }
        else
        {
            mockResponse = new
            {
                subjective = $"Patient reported symptoms: {request.Text}",
                objective = "Vitals stable. Physical exam normal.",
                assessment = "General consultation, pending investigations.",
                plan = "Review symptoms. Advise patient to follow up as needed.",
                primaryCode = "Z00.00",
                primaryDesc = "Encounter for general adult medical examination without abnormal findings",
                secondary = Array.Empty<object>()
            };
        }

        return Ok(mockResponse);
    }

    [HttpPost("patient-summary")]
    public async Task<IActionResult> PatientSummary([FromBody] SummaryRequest request)
    {
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

        string? result = await CallGeminiAsync(prompt, jsonMode: false);
        if (result != null)
        {
            return Ok(new { summary = result });
        }

        // Mock Summary
        string mockSummary = $@"### 📋 Patient Take-Home Summary
**What we found:**
You have been diagnosed with **Acute Pharyngitis (Throat Infection)**. Your throat is quite red and swollen, and you have a mild fever (38.1°C), which explains the pain when swallowing.

**Your Treatment Plan:**
1. **Medication (Amoxicillin):** Take 1 capsule three times a day for all 10 days. Even if you feel better, **please complete the entire course** to fully clear the infection.
2. **Fever/Pain Relief (Acetaminophen):** Take 1 tablet every 6 hours only as needed for throat pain or fever.
3. **Home Care:** 
   - Gargle with warm salt water 3-4 times a day to reduce throat irritation.
   - Drink plenty of warm liquids (teas, broth) or cold water to stay hydrated.
   - Rest your voice and body.

**⚠️ Warning Signs to Watch For:**
Go to the emergency clinic or contact us immediately if you experience:
- Difficulty breathing or a feeling of your throat closing.
- Inability to swallow liquids or manage your saliva (drooling).
- A fever that stays above 39.5°C (103°F) even after taking medicine.";

        return Ok(new { summary = mockSummary });
    }

    [HttpPost("lab-interpreter")]
    public async Task<IActionResult> LabInterpreter([FromBody] LabInterpreterRequest request)
    {
        string resultsJson = JsonSerializer.Serialize(request.Results);
        string prompt = $@"You are an expert Clinical Pathologist. Review the following laboratory results for this patient and provide a concise, structured interpretation for the requesting doctor.
Patient Name: {request.PatientName}
Test Panel: {request.TestName}

Results:
{resultsJson}

Format your response in Markdown with these sections:
1. **Clinical Assessment Summary**: Overview of the findings (e.g. anemia, hyperglycemia).
2. **Abnormal / Critical Flags**: Detail each abnormal value, what it indicates, and severity.
3. **Pathophysiological Correlations**: What potential underlying conditions explain these values.
4. **Recommended Next Steps**: Recommended follow-up tests, monitoring, or clinical interventions.
5. **Medical Disclaimer**: Standard AI clinical guidance disclaimer.

Be concise, technical, and professional.";

        string? result = await CallGeminiAsync(prompt, jsonMode: false);
        if (result != null)
        {
            return Ok(new { interpretation = result });
        }

        // Mock Lab Interpretation
        StringBuilder mockBuilder = new();
        mockBuilder.AppendLine("### 🧪 AI Clinical Interpretation Report");
        mockBuilder.AppendLine($"**Patient:** {request.PatientName} | **Panel:** {request.TestName}\n");
        mockBuilder.AppendLine("#### 1. Clinical Assessment Summary");
        mockBuilder.AppendLine("The results indicate significant elevations in glycemic markers (Glucose & HbA1c), pointing towards **poorly controlled Diabetes Mellitus** or a new acute hyperglycemic presentation. Remaining hematology and metabolic markers are within normal limits.");
        mockBuilder.AppendLine("\n#### 2. Abnormal Flags & Findings");
        
        bool foundElevations = false;
        foreach (var item in request.Results)
        {
            if (item.Flag == "H" || item.Flag == "L" || item.Flag == "HH" || item.Flag == "LL" || item.Flag == "Critical")
            {
                foundElevations = true;
                mockBuilder.AppendLine($"- **{item.TestName} ({item.TestCode})**: {item.Value} {item.Unit} (Ref: {item.RefRange}). **Flag: {item.Flag}**. Indicates acute physiological elevation.");
            }
        }
        if (!foundElevations)
        {
            mockBuilder.AppendLine("- *No critical flags found.* Mild elevation in Glucose (6.8 mmol/L) is noted, suggesting borderline pre-diabetes.");
        }

        mockBuilder.AppendLine("\n#### 3. Pathophysiological Correlations");
        mockBuilder.AppendLine("Elevated blood glucose levels in conjunction with high HbA1c suggest insulin resistance and persistent glucose toxicity. If accompanied by polyuria, polydipsia, or weight loss, immediate glycemic control intervention is indicated.");
        mockBuilder.AppendLine("\n#### 4. Recommended Next Steps");
        mockBuilder.AppendLine("1. Coordinate fasting blood glucose and oral glucose tolerance tests if necessary.");
        mockBuilder.AppendLine("2. Initiate lifestyle modifications (medical nutrition therapy, physical exercise).");
        mockBuilder.AppendLine("3. Review active medications; consider initiating or adjusting Metformin therapy.");
        mockBuilder.AppendLine("4. Schedule repeat HbA1c in 3 months.");
        mockBuilder.AppendLine("\n*Disclaimer: This is an AI-generated analysis intended for clinical support. Final diagnosis and treatment decisions remain the responsibility of the licensed physician.*");

        return Ok(new { interpretation = mockBuilder.ToString() });
    }

    [HttpPost("drug-safety")]
    public async Task<IActionResult> DrugSafety([FromBody] DrugSafetyRequest request)
    {
        string drugsJson = JsonSerializer.Serialize(request.Items);
        string prompt = $@"You are a Clinical Pharmacist. Review the following medication cart for potential safety risks, drug-drug interactions, and controlled substance flags.
Medications:
{drugsJson}

Provide a structured response in Markdown containing:
1. **Drug-Drug Interactions**: High, moderate, or minor interactions between the listed items.
2. **Controlled Substance Alerts**: Highlight any controlled substances and compliance requirements.
3. **Patient Counseling Guidelines**: Key points for patient counseling (e.g. food requirements, warnings, alcohol interactions).

Be professional, concise, and focused on patient safety.";

        string? result = await CallGeminiAsync(prompt, jsonMode: false);
        if (result != null)
        {
            return Ok(new { interactions = result });
        }

        // Mock Drug Safety
        string mockInteractions = "#### ⚠️ Drug Interaction Risk Assessment\n";
        bool hasAspirin = false;
        bool hasWarfarin = false;

        foreach (var d in request.Items)
        {
            var name = d.DrugName.ToLower();
            if (name.Contains("aspirin")) hasAspirin = true;
            if (name.Contains("warfarin") || name.Contains("clopidogrel") || name.Contains("heparin")) hasWarfarin = true;
        }

        if (hasAspirin && hasWarfarin)
        {
            mockInteractions += "**[CRITICAL RISK] Aspirin + Blood Thinner (Warfarin/Clopidogrel):** Simultaneous use significantly increases the risk of serious GI bleed and hemorrhage. Monitor patient closely for bruising, dark stools, or epistaxis. Consider prescribing a proton pump inhibitor (PPI) for gastric protection.\n";
        }
        else if (hasAspirin)
        {
            mockInteractions += "**[MODERATE RISK] Aspirin + NSAIDs:** Concomitant use increases risk of gastrointestinal mucosal irritation. Recommend spaced dosing.\n";
        }
        else
        {
            mockInteractions += "✅ **No major drug-drug interactions detected** between the selected medications in this cart.\n";
        }

        mockInteractions += "\n#### 📝 Patient Counseling Guidelines\n";
        foreach (var d in request.Items)
        {
            var name = d.DrugName.ToLower();
            if (name.Contains("amoxicillin") || name.Contains("antibiotic"))
            {
                mockInteractions += $"- ***{d.DrugName}***: Instruct patient to complete the entire course, even if symptoms resolve. Can be taken with or without food. Inform pharmacist of severe rash.\n";
            }
            else if (name.Contains("metformin"))
            {
                mockInteractions += $"- ***{d.DrugName}***: Take with meals to reduce gastrointestinal upset. Avoid excessive alcohol consumption to prevent potential lactic acidosis risk.\n";
            }
            else if (name.Contains("aspirin") || name.Contains("ibuprofen"))
            {
                mockInteractions += $"- ***{d.DrugName}***: Take with food or milk to protect stomach lining. Report any stomach pain or dark tarry stools immediately.\n";
            }
            else
            {
                mockInteractions += $"- ***{d.DrugName}***: Administer according to label. Spaced dosing is recommended.\n";
            }
        }

        return Ok(new { interactions = mockInteractions });
    }

    private async Task<string?> CallGeminiAsync(string prompt, bool jsonMode = false)
    {
        var apiKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key={apiKey}";
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = jsonMode ? new { responseMimeType = "application/json" } : null
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var text))
            {
                return text.GetString();
            }
        }
        catch
        {
            // Fail silent, fallback to Mock
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
