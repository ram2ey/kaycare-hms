-- ============================================================
-- TENANT MANAGEMENT
-- ============================================================

CREATE TABLE Tenants (
    TenantId        UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantCode      NVARCHAR(50)        NOT NULL,
    TenantName      NVARCHAR(200)       NOT NULL,
    Subdomain       NVARCHAR(100)       NOT NULL,
    SubscriptionPlan NVARCHAR(50)       NOT NULL DEFAULT 'standard',
    IsActive        BIT                 NOT NULL DEFAULT 1,
    MaxUsers        INT                 NOT NULL DEFAULT 50,
    StorageQuotaGB  INT                 NOT NULL DEFAULT 100,
    CreatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Tenants PRIMARY KEY (TenantId),
    CONSTRAINT UQ_Tenants_TenantCode UNIQUE (TenantCode),
    CONSTRAINT UQ_Tenants_Subdomain UNIQUE (Subdomain)
);

-- ============================================================
-- ROLES
-- ============================================================

CREATE TABLE Roles (
    RoleId          INT                 NOT NULL IDENTITY(1,1),
    RoleName        NVARCHAR(50)        NOT NULL,
    Description     NVARCHAR(200)       NULL,

    CONSTRAINT PK_Roles PRIMARY KEY (RoleId),
    CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
);

INSERT INTO Roles (RoleName, Description) VALUES
('SuperAdmin',   'Platform-level administrator'),
('Admin',        'Hospital administrator'),
('Doctor',       'Licensed physician'),
('Nurse',        'Nursing staff'),
('Receptionist', 'Front desk / patient registration'),
('Pharmacist',   'Pharmacy staff');

-- ============================================================
-- USERS
-- ============================================================

CREATE TABLE Users (
    UserId              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    RoleId              INT                 NOT NULL,
    Email               NVARCHAR(256)       NOT NULL,
    PasswordHash        NVARCHAR(512)       NOT NULL,
    FirstName           NVARCHAR(100)       NOT NULL,
    LastName            NVARCHAR(100)       NOT NULL,
    PhoneNumber         NVARCHAR(20)        NULL,
    LicenseNumber       NVARCHAR(100)       NULL,
    Department          NVARCHAR(100)       NULL,
    IsActive            BIT                 NOT NULL DEFAULT 1,
    MustChangePassword  BIT                 NOT NULL DEFAULT 1,
    LastLoginAt         DATETIME2           NULL,
    FailedLoginCount    INT                 NOT NULL DEFAULT 0,
    LockedUntil         DATETIME2           NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedByUserId     UNIQUEIDENTIFIER    NULL,

    CONSTRAINT PK_Users PRIMARY KEY (UserId),
    CONSTRAINT FK_Users_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId),
    CONSTRAINT UQ_Users_Email_Tenant UNIQUE (TenantId, Email)
);

-- ============================================================
-- PATIENTS
-- ============================================================

CREATE TABLE Patients (
    PatientId               UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId                UNIQUEIDENTIFIER    NOT NULL,
    MedicalRecordNumber     NVARCHAR(50)        NOT NULL,
    FirstName               NVARCHAR(100)       NOT NULL,
    MiddleName              NVARCHAR(100)       NULL,
    LastName                NVARCHAR(100)       NOT NULL,
    DateOfBirth             DATE                NOT NULL,
    Gender                  NVARCHAR(20)        NOT NULL,
    BloodType               NVARCHAR(5)         NULL,
    NationalId              NVARCHAR(50)        NULL,

    -- Contact
    Email                   NVARCHAR(256)       NULL,
    PhoneNumber             NVARCHAR(20)        NULL,
    AlternatePhone          NVARCHAR(20)        NULL,

    -- Address
    AddressLine1            NVARCHAR(200)       NULL,
    AddressLine2            NVARCHAR(200)       NULL,
    City                    NVARCHAR(100)       NULL,
    State                   NVARCHAR(100)       NULL,
    PostalCode              NVARCHAR(20)        NULL,
    Country                 NVARCHAR(100)       NULL DEFAULT 'GH',

    -- Emergency Contact
    EmergencyContactName    NVARCHAR(200)       NULL,
    EmergencyContactPhone   NVARCHAR(20)        NULL,
    EmergencyContactRelation NVARCHAR(50)       NULL,

    -- Insurance
    InsuranceProvider       NVARCHAR(200)       NULL,
    InsurancePolicyNumber   NVARCHAR(100)       NULL,
    InsuranceGroupNumber    NVARCHAR(100)       NULL,

    -- Flags
    HasAllergies            BIT                 NOT NULL DEFAULT 0,
    HasChronicConditions    BIT                 NOT NULL DEFAULT 0,

    IsActive                BIT                 NOT NULL DEFAULT 1,
    RegisteredAt            DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    RegisteredByUserId      UNIQUEIDENTIFIER    NOT NULL,
    UpdatedAt               DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Patients PRIMARY KEY (PatientId),
    CONSTRAINT FK_Patients_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT UQ_Patients_MRN_Tenant UNIQUE (TenantId, MedicalRecordNumber)
);

CREATE TABLE PatientAllergies (
    AllergyId           UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    PatientId           UNIQUEIDENTIFIER    NOT NULL,
    AllergyType         NVARCHAR(50)        NOT NULL,
    AllergenName        NVARCHAR(200)       NOT NULL,
    Reaction            NVARCHAR(500)       NULL,
    Severity            NVARCHAR(20)        NOT NULL,
    RecordedAt          DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    RecordedByUserId    UNIQUEIDENTIFIER    NOT NULL,

    CONSTRAINT PK_PatientAllergies PRIMARY KEY (AllergyId),
    CONSTRAINT FK_Allergies_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_Allergies_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId)
);

-- ============================================================
-- APPOINTMENTS
-- ============================================================

CREATE TABLE Appointments (
    AppointmentId       UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    PatientId           UNIQUEIDENTIFIER    NOT NULL,
    DoctorUserId        UNIQUEIDENTIFIER    NOT NULL,
    ScheduledAt         DATETIME2           NOT NULL,
    DurationMinutes     INT                 NOT NULL DEFAULT 30,
    AppointmentType     NVARCHAR(50)        NOT NULL,
    Status              NVARCHAR(50)        NOT NULL DEFAULT 'Scheduled',
    ChiefComplaint      NVARCHAR(1000)      NULL,
    Room                NVARCHAR(50)        NULL,
    Notes               NVARCHAR(2000)      NULL,
    CancelledAt         DATETIME2           NULL,
    CancelledByUserId   UNIQUEIDENTIFIER    NULL,
    CancellationReason  NVARCHAR(500)       NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    CreatedByUserId     UNIQUEIDENTIFIER    NOT NULL,
    UpdatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Appointments PRIMARY KEY (AppointmentId),
    CONSTRAINT FK_Appointments_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorUserId) REFERENCES Users(UserId)
);

-- ============================================================
-- CONSULTATIONS (SOAP Notes)
-- ============================================================

CREATE TABLE Consultations (
    ConsultationId          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId                UNIQUEIDENTIFIER    NOT NULL,
    AppointmentId           UNIQUEIDENTIFIER    NOT NULL,
    PatientId               UNIQUEIDENTIFIER    NOT NULL,
    DoctorUserId            UNIQUEIDENTIFIER    NOT NULL,

    -- SOAP
    SubjectiveNotes         NVARCHAR(MAX)       NULL,
    ObjectiveNotes          NVARCHAR(MAX)       NULL,
    AssessmentNotes         NVARCHAR(MAX)       NULL,
    PlanNotes               NVARCHAR(MAX)       NULL,

    -- Vitals
    BloodPressureSystolic   INT                 NULL,
    BloodPressureDiastolic  INT                 NULL,
    HeartRateBPM            INT                 NULL,
    TemperatureCelsius      DECIMAL(4,1)        NULL,
    WeightKg                DECIMAL(5,2)        NULL,
    HeightCm                DECIMAL(5,1)        NULL,
    OxygenSaturationPct     DECIMAL(4,1)        NULL,

    -- Diagnosis
    PrimaryDiagnosisCode    NVARCHAR(20)        NULL,
    PrimaryDiagnosisDesc    NVARCHAR(500)       NULL,
    SecondaryDiagnoses      NVARCHAR(MAX)       NULL,

    Status                  NVARCHAR(50)        NOT NULL DEFAULT 'Draft',
    SignedAt                DATETIME2           NULL,
    CreatedAt               DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt               DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Consultations PRIMARY KEY (ConsultationId),
    CONSTRAINT FK_Consultations_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Consultations_Appointments FOREIGN KEY (AppointmentId) REFERENCES Appointments(AppointmentId)
);

-- ============================================================
-- PRESCRIPTIONS
-- ============================================================

CREATE TABLE Prescriptions (
    PrescriptionId      UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    ConsultationId      UNIQUEIDENTIFIER    NOT NULL,
    PatientId           UNIQUEIDENTIFIER    NOT NULL,
    PrescribedByUserId  UNIQUEIDENTIFIER    NOT NULL,
    PrescriptionDate    DATE                NOT NULL DEFAULT CAST(SYSUTCDATETIME() AS DATE),
    Status              NVARCHAR(50)        NOT NULL DEFAULT 'Active',
    Notes               NVARCHAR(1000)      NULL,
    DispensedAt         DATETIME2           NULL,
    DispensedByUserId   UNIQUEIDENTIFIER    NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Prescriptions PRIMARY KEY (PrescriptionId),
    CONSTRAINT FK_Prescriptions_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Prescriptions_Consultations FOREIGN KEY (ConsultationId) REFERENCES Consultations(ConsultationId)
);

CREATE TABLE PrescriptionItems (
    ItemId              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    PrescriptionId      UNIQUEIDENTIFIER    NOT NULL,
    MedicationName      NVARCHAR(200)       NOT NULL,
    GenericName         NVARCHAR(200)       NULL,
    Strength            NVARCHAR(100)       NOT NULL,
    DosageForm          NVARCHAR(50)        NOT NULL,
    Frequency           NVARCHAR(100)       NOT NULL,
    DurationDays        INT                 NOT NULL,
    Quantity            INT                 NOT NULL,
    Refills             INT                 NOT NULL DEFAULT 0,
    Instructions        NVARCHAR(500)       NULL,
    IsControlledSubstance BIT              NOT NULL DEFAULT 0,

    CONSTRAINT PK_PrescriptionItems PRIMARY KEY (ItemId),
    CONSTRAINT FK_PrescItems_Prescriptions FOREIGN KEY (PrescriptionId) REFERENCES Prescriptions(PrescriptionId)
);

-- ============================================================
-- BILLING
-- ============================================================

CREATE TABLE Bills (
    BillId              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    PatientId           UNIQUEIDENTIFIER    NOT NULL,
    AppointmentId       UNIQUEIDENTIFIER    NULL,
    BillNumber          NVARCHAR(50)        NOT NULL,
    BillDate            DATE                NOT NULL DEFAULT CAST(SYSUTCDATETIME() AS DATE),
    DueDate             DATE                NOT NULL,
    Status              NVARCHAR(50)        NOT NULL DEFAULT 'Draft',
    SubtotalAmount      DECIMAL(12,2)       NOT NULL DEFAULT 0,
    DiscountAmount      DECIMAL(12,2)       NOT NULL DEFAULT 0,
    TaxAmount           DECIMAL(12,2)       NOT NULL DEFAULT 0,
    TotalAmount         DECIMAL(12,2)       NOT NULL DEFAULT 0,
    PaidAmount          DECIMAL(12,2)       NOT NULL DEFAULT 0,
    BalanceDue          AS (TotalAmount - PaidAmount),
    Currency            NVARCHAR(3)         NOT NULL DEFAULT 'GHS',
    Notes               NVARCHAR(1000)      NULL,
    CreatedByUserId     UNIQUEIDENTIFIER    NOT NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Bills PRIMARY KEY (BillId),
    CONSTRAINT FK_Bills_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Bills_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT UQ_Bills_Number_Tenant UNIQUE (TenantId, BillNumber)
);

CREATE TABLE BillItems (
    BillItemId          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    BillId              UNIQUEIDENTIFIER    NOT NULL,
    ServiceCode         NVARCHAR(50)        NULL,
    Description         NVARCHAR(500)       NOT NULL,
    Quantity            DECIMAL(8,2)        NOT NULL DEFAULT 1,
    UnitPrice           DECIMAL(10,2)       NOT NULL,
    TotalPrice          AS (Quantity * UnitPrice),

    CONSTRAINT PK_BillItems PRIMARY KEY (BillItemId),
    CONSTRAINT FK_BillItems_Bills FOREIGN KEY (BillId) REFERENCES Bills(BillId)
);

CREATE TABLE Payments (
    PaymentId           UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    BillId              UNIQUEIDENTIFIER    NOT NULL,
    PaymentDate         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    Amount              DECIMAL(12,2)       NOT NULL,
    PaymentMethod       NVARCHAR(50)        NOT NULL,
    ReferenceNumber     NVARCHAR(200)       NULL,
    Notes               NVARCHAR(500)       NULL,
    RecordedByUserId    UNIQUEIDENTIFIER    NOT NULL,
    CreatedAt           DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_Payments PRIMARY KEY (PaymentId),
    CONSTRAINT FK_Payments_Bills FOREIGN KEY (BillId) REFERENCES Bills(BillId),
    CONSTRAINT FK_Payments_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId)
);

-- ============================================================
-- DOCUMENTS
-- ============================================================

CREATE TABLE Documents (
    DocumentId          UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId            UNIQUEIDENTIFIER    NOT NULL,
    PatientId           UNIQUEIDENTIFIER    NOT NULL,
    ConsultationId      UNIQUEIDENTIFIER    NULL,
    DocumentType        NVARCHAR(50)        NOT NULL,
    DocumentName        NVARCHAR(300)       NOT NULL,
    BlobContainerName   NVARCHAR(200)       NOT NULL,
    BlobPath            NVARCHAR(1000)      NOT NULL,
    FileSizeBytes       BIGINT              NOT NULL,
    MimeType            NVARCHAR(100)       NOT NULL,
    IsEncrypted         BIT                 NOT NULL DEFAULT 1,
    UploadedAt          DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    UploadedByUserId    UNIQUEIDENTIFIER    NOT NULL,

    CONSTRAINT PK_Documents PRIMARY KEY (DocumentId),
    CONSTRAINT FK_Documents_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Documents_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId)
);

-- ============================================================
-- LAB RESULTS
-- ============================================================

CREATE TABLE LabResults (
    LabResultId             UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantId                UNIQUEIDENTIFIER    NOT NULL,
    PatientId               UNIQUEIDENTIFIER    NOT NULL,
    OrderingDoctorUserId    UNIQUEIDENTIFIER    NULL,
    InstrumentSource        NVARCHAR(50)        NOT NULL,
    AccessionNumber         NVARCHAR(100)       NULL,
    TestCode                NVARCHAR(50)        NOT NULL,
    TestName                NVARCHAR(200)       NOT NULL,
    ResultValue             NVARCHAR(100)       NOT NULL,
    ResultUnit              NVARCHAR(50)        NULL,
    ReferenceRangeLow       DECIMAL(10,3)       NULL,
    ReferenceRangeHigh      DECIMAL(10,3)       NULL,
    AbnormalFlag            NVARCHAR(10)        NULL,
    ResultStatus            NVARCHAR(20)        NOT NULL DEFAULT 'Final',
    CollectedAt             DATETIME2           NULL,
    ResultedAt              DATETIME2           NOT NULL,
    IsReviewedByDoctor      BIT                 NOT NULL DEFAULT 0,
    ReviewedAt              DATETIME2           NULL,
    ReviewedByUserId        UNIQUEIDENTIFIER    NULL,
    CreatedAt               DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_LabResults PRIMARY KEY (LabResultId),
    CONSTRAINT FK_LabResults_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_LabResults_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId)
);

-- ============================================================
-- AUDIT LOGS (HIPAA Required — never delete these)
-- ============================================================

CREATE TABLE AuditLogs (
    AuditLogId      BIGINT              NOT NULL IDENTITY(1,1),
    TenantId        UNIQUEIDENTIFIER    NOT NULL,
    UserId          UNIQUEIDENTIFIER    NULL,
    Action          NVARCHAR(100)       NOT NULL,
    EntityType      NVARCHAR(100)       NULL,
    EntityId        NVARCHAR(100)       NULL,
    OldValues       NVARCHAR(MAX)       NULL,
    NewValues       NVARCHAR(MAX)       NULL,
    IpAddress       NVARCHAR(45)        NULL,
    UserAgent       NVARCHAR(500)       NULL,
    IsSuccess       BIT                 NOT NULL DEFAULT 1,
    ErrorMessage    NVARCHAR(1000)      NULL,
    Timestamp       DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_AuditLogs PRIMARY KEY (AuditLogId)
    -- No FK on TenantId intentionally — audit logs survive tenant deletion
);

-- ============================================================
-- INDEXES
-- ============================================================

CREATE INDEX IX_Patients_TenantId
    ON Patients(TenantId) INCLUDE (FirstName, LastName, DateOfBirth);

CREATE INDEX IX_Patients_TenantId_LastName
    ON Patients(TenantId, LastName);

CREATE INDEX IX_Patients_MRN
    ON Patients(TenantId, MedicalRecordNumber);

CREATE INDEX IX_Appointments_TenantId_Doctor_Date
    ON Appointments(TenantId, DoctorUserId, ScheduledAt);

CREATE INDEX IX_Appointments_TenantId_Patient
    ON Appointments(TenantId, PatientId);

CREATE INDEX IX_LabResults_TenantId_Patient
    ON LabResults(TenantId, PatientId);

CREATE INDEX IX_LabResults_AbnormalFlag
    ON LabResults(TenantId, AbnormalFlag) WHERE AbnormalFlag IS NOT NULL;

CREATE INDEX IX_AuditLogs_TenantId_Timestamp
    ON AuditLogs(TenantId, Timestamp DESC);

CREATE INDEX IX_AuditLogs_UserId_Timestamp
    ON AuditLogs(UserId, Timestamp DESC);

CREATE INDEX IX_AuditLogs_EntityType_EntityId
    ON AuditLogs(EntityType, EntityId);

-- ============================================================
-- ROW-LEVEL SECURITY (Database-enforced tenant isolation)
-- ============================================================

CREATE FUNCTION dbo.fn_TenantAccessPredicate(@TenantId UNIQUEIDENTIFIER)
    RETURNS TABLE
    WITH SCHEMABINDING
AS
    RETURN SELECT 1 AS AccessResult
    WHERE @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER)
       OR IS_MEMBER('db_owner') = 1;

CREATE SECURITY POLICY TenantIsolationPolicy
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.Patients,
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.Appointments,
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.Consultations,
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.Prescriptions,
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.Bills,
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.Documents,
    ADD FILTER PREDICATE dbo.fn_TenantAccessPredicate(TenantId) ON dbo.LabResults
WITH (STATE = ON);