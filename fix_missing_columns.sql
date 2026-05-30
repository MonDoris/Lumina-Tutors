-- ============================================================
-- fix_missing_columns.sql  (idempotent — safe to re-run)
-- ============================================================

-- Verify we're on the right database
PRINT 'Database: ' + DB_NAME();

-- ── 1. OnlineSessions ─────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OnlineSessions')
BEGIN
    CREATE TABLE OnlineSessions (
        SessionId       INT            IDENTITY(1,1) NOT NULL,
        SchoolId        INT            NOT NULL,
        TeacherId       INT            NOT NULL,
        Title           NVARCHAR(200)  NOT NULL,
        Description     NVARCHAR(1000) NULL,
        RoomCode        NVARCHAR(20)   NOT NULL,
        Status          NVARCHAR(20)   NOT NULL CONSTRAINT DF_OnlineSessions_Status    DEFAULT 'Scheduled',
        ScheduledAt     DATETIME2      NULL,
        StartedAt       DATETIME2      NULL,
        EndedAt         DATETIME2      NULL,
        MaxParticipants INT            NOT NULL CONSTRAINT DF_OnlineSessions_MaxPart   DEFAULT 50,
        CreatedAt       DATETIME2      NOT NULL CONSTRAINT DF_OnlineSessions_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt       DATETIME2      NOT NULL CONSTRAINT DF_OnlineSessions_UpdatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT PK_OnlineSessions PRIMARY KEY (SessionId),
        CONSTRAINT UQ_OnlineSessions_School_RoomCode UNIQUE (SchoolId, RoomCode)
    );
    CREATE INDEX IX_OnlineSessions_TeacherId ON OnlineSessions (TeacherId);
    PRINT 'OnlineSessions: table created';
END
ELSE
    PRINT 'OnlineSessions: already exists';

-- Add FKs to OnlineSessions only if parent tables exist
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Schools')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OnlineSessions_Schools')
BEGIN
    ALTER TABLE OnlineSessions
        ADD CONSTRAINT FK_OnlineSessions_Schools
        FOREIGN KEY (SchoolId) REFERENCES Schools (SchoolId);
    PRINT 'FK_OnlineSessions_Schools: added';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OnlineSessions_Users')
BEGIN
    ALTER TABLE OnlineSessions
        ADD CONSTRAINT FK_OnlineSessions_Users
        FOREIGN KEY (TeacherId) REFERENCES Users (UserId);
    PRINT 'FK_OnlineSessions_Users: added';
END

-- ── 2. SessionParticipants ────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SessionParticipants')
BEGIN
    CREATE TABLE SessionParticipants (
        ParticipantId INT       IDENTITY(1,1) NOT NULL,
        SessionId     INT       NOT NULL,
        UserId        INT       NOT NULL,
        JoinedAt      DATETIME2 NOT NULL CONSTRAINT DF_SP_JoinedAt  DEFAULT GETUTCDATE(),
        LeftAt        DATETIME2 NULL,
        IsAttended    BIT       NOT NULL CONSTRAINT DF_SP_IsAttended DEFAULT 0,
        AttendedAt    DATETIME2 NULL,
        CONSTRAINT PK_SessionParticipants PRIMARY KEY (ParticipantId)
    );
    CREATE INDEX IX_SessionParticipants_Session_User ON SessionParticipants (SessionId, UserId);
    CREATE INDEX IX_SessionParticipants_UserId        ON SessionParticipants (UserId);
    PRINT 'SessionParticipants: table created';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('SessionParticipants') AND name = 'IsAttended')
    BEGIN
        ALTER TABLE SessionParticipants
            ADD IsAttended BIT NOT NULL CONSTRAINT DF_SP_IsAttended DEFAULT 0;
        PRINT 'SessionParticipants: added IsAttended';
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('SessionParticipants') AND name = 'AttendedAt')
    BEGIN
        ALTER TABLE SessionParticipants ADD AttendedAt DATETIME2 NULL;
        PRINT 'SessionParticipants: added AttendedAt';
    END
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OnlineSessions')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SessionParticipants_Sessions')
BEGIN
    ALTER TABLE SessionParticipants
        ADD CONSTRAINT FK_SessionParticipants_Sessions
        FOREIGN KEY (SessionId) REFERENCES OnlineSessions (SessionId) ON DELETE CASCADE;
    PRINT 'FK_SessionParticipants_Sessions: added';
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SessionParticipants_Users')
BEGIN
    ALTER TABLE SessionParticipants
        ADD CONSTRAINT FK_SessionParticipants_Users
        FOREIGN KEY (UserId) REFERENCES Users (UserId);
    PRINT 'FK_SessionParticipants_Users: added';
END

-- ── 3. QuestionBanks — add missing columns ────────────────────
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'QuestionBanks')
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('QuestionBanks') AND name = 'CorrectAnswer')
    BEGIN
        ALTER TABLE QuestionBanks ADD CorrectAnswer NVARCHAR(MAX) NULL;
        PRINT 'QuestionBanks: added CorrectAnswer';
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('QuestionBanks') AND name = 'SourceUrl')
    BEGIN
        ALTER TABLE QuestionBanks ADD SourceUrl NVARCHAR(500) NULL;
        PRINT 'QuestionBanks: added SourceUrl';
    END
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                   WHERE object_id = OBJECT_ID('QuestionBanks') AND name = 'Tags')
    BEGIN
        ALTER TABLE QuestionBanks ADD Tags NVARCHAR(500) NULL;
        PRINT 'QuestionBanks: added Tags';
    END
END
ELSE
    PRINT 'QuestionBanks: table not found — skipping column additions';

-- ── 4. OnlineRoomChats ────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OnlineRoomChats')
BEGIN
    CREATE TABLE OnlineRoomChats (
        ChatId      INT            IDENTITY(1,1) NOT NULL,
        SessionId   INT            NOT NULL,
        SenderId    INT            NOT NULL,
        Content     NVARCHAR(2000) NOT NULL,
        MessageType NVARCHAR(20)   NOT NULL CONSTRAINT DF_ORC_MessageType DEFAULT 'Text',
        SentAt      DATETIME2      NOT NULL CONSTRAINT DF_ORC_SentAt       DEFAULT GETUTCDATE(),
        CONSTRAINT PK_OnlineRoomChats PRIMARY KEY (ChatId)
    );
    CREATE INDEX IX_OnlineRoomChats_SessionId_SentAt ON OnlineRoomChats (SessionId, SentAt);
    CREATE INDEX IX_OnlineRoomChats_SenderId          ON OnlineRoomChats (SenderId);
    PRINT 'OnlineRoomChats: table created';
END
ELSE
    PRINT 'OnlineRoomChats: already exists';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OnlineSessions')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OnlineRoomChats_Sessions')
BEGIN
    ALTER TABLE OnlineRoomChats
        ADD CONSTRAINT FK_OnlineRoomChats_Sessions
        FOREIGN KEY (SessionId) REFERENCES OnlineSessions (SessionId) ON DELETE CASCADE;
END

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OnlineRoomChats_Users')
BEGIN
    ALTER TABLE OnlineRoomChats
        ADD CONSTRAINT FK_OnlineRoomChats_Users
        FOREIGN KEY (SenderId) REFERENCES Users (UserId);
END

-- ── 5. OnlineSlides ───────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OnlineSlides')
BEGIN
    CREATE TABLE OnlineSlides (
        SlideId    INT           IDENTITY(1,1) NOT NULL,
        SessionId  INT           NOT NULL,
        FileName   NVARCHAR(260) NOT NULL,
        FileUrl    NVARCHAR(500) NOT NULL,
        TotalPages INT           NOT NULL CONSTRAINT DF_OS_TotalPages DEFAULT 1,
        UploadedAt DATETIME2     NOT NULL CONSTRAINT DF_OS_UploadedAt DEFAULT GETUTCDATE(),
        CONSTRAINT PK_OnlineSlides PRIMARY KEY (SlideId)
    );
    CREATE INDEX IX_OnlineSlides_SessionId ON OnlineSlides (SessionId);
    PRINT 'OnlineSlides: table created';
END
ELSE
    PRINT 'OnlineSlides: already exists';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OnlineSessions')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_OnlineSlides_Sessions')
BEGIN
    ALTER TABLE OnlineSlides
        ADD CONSTRAINT FK_OnlineSlides_Sessions
        FOREIGN KEY (SessionId) REFERENCES OnlineSessions (SessionId) ON DELETE CASCADE;
END

-- ── 6. QuestionImportJobs ─────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'QuestionImportJobs')
BEGIN
    CREATE TABLE QuestionImportJobs (
        ImportJobId       INT            IDENTITY(1,1) NOT NULL,
        SchoolId          INT            NOT NULL,
        RequestedByUserId INT            NOT NULL,
        TargetSubjectId   INT            NOT NULL,
        SourceUrl         NVARCHAR(500)  NOT NULL,
        Status            NVARCHAR(20)   NOT NULL CONSTRAINT DF_QIJ_Status    DEFAULT 'Pending',
        ImportedCount     INT            NOT NULL CONSTRAINT DF_QIJ_Count     DEFAULT 0,
        ErrorMessage      NVARCHAR(1000) NULL,
        ProcessedAt       DATETIME2      NULL,
        CreatedAt         DATETIME2      NOT NULL CONSTRAINT DF_QIJ_CreatedAt DEFAULT GETUTCDATE(),
        UpdatedAt         DATETIME2      NOT NULL CONSTRAINT DF_QIJ_UpdatedAt DEFAULT GETUTCDATE(),
        CONSTRAINT PK_QuestionImportJobs PRIMARY KEY (ImportJobId)
    );
    CREATE INDEX IX_QuestionImportJobs_SchoolId          ON QuestionImportJobs (SchoolId);
    CREATE INDEX IX_QuestionImportJobs_RequestedByUserId ON QuestionImportJobs (RequestedByUserId);
    CREATE INDEX IX_QuestionImportJobs_TargetSubjectId   ON QuestionImportJobs (TargetSubjectId);
    PRINT 'QuestionImportJobs: table created';
END
ELSE
    PRINT 'QuestionImportJobs: already exists';

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Schools')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_QuestionImportJobs_Schools')
    ALTER TABLE QuestionImportJobs ADD CONSTRAINT FK_QuestionImportJobs_Schools
        FOREIGN KEY (SchoolId) REFERENCES Schools (SchoolId);

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_QuestionImportJobs_Users')
    ALTER TABLE QuestionImportJobs ADD CONSTRAINT FK_QuestionImportJobs_Users
        FOREIGN KEY (RequestedByUserId) REFERENCES Users (UserId);

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Subjects')
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_QuestionImportJobs_Subjects')
    ALTER TABLE QuestionImportJobs ADD CONSTRAINT FK_QuestionImportJobs_Subjects
        FOREIGN KEY (TargetSubjectId) REFERENCES Subjects (SubjectId);

-- ── 7. Mark migrations as applied ────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '__EFMigrationsHistory')
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        MigrationId    NVARCHAR(150) NOT NULL,
        ProductVersion NVARCHAR(32)  NOT NULL,
        CONSTRAINT PK___EFMigrationsHistory PRIMARY KEY (MigrationId)
    );
    PRINT '__EFMigrationsHistory: table created';
END

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
               WHERE MigrationId = '20260528162224_AddOnlineClassroom')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES ('20260528162224_AddOnlineClassroom', '8.0.27');

IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
               WHERE MigrationId = '20260529000000_AddOnlineClassroomFull')
    INSERT INTO [__EFMigrationsHistory] (MigrationId, ProductVersion)
    VALUES ('20260529000000_AddOnlineClassroomFull', '8.0.27');

PRINT '=== Done ===';

-- ── 8. AssignmentAttachments ─────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AssignmentAttachments')
BEGIN
    CREATE TABLE AssignmentAttachments (
        AttachmentId INT            IDENTITY(1,1) NOT NULL,
        AssignmentId INT            NOT NULL,
        FileName     NVARCHAR(255)  NOT NULL,
        FileUrl      NVARCHAR(500)  NOT NULL,
        FileType     NVARCHAR(50)   NOT NULL,
        FileSizeKB   INT            NULL,
        UploadedAt   DATETIME2      NOT NULL CONSTRAINT DF_AA_UploadedAt DEFAULT GETUTCDATE(),
        CONSTRAINT PK_AssignmentAttachments PRIMARY KEY (AttachmentId)
    );
    CREATE INDEX IX_AssignmentAttachments_AssignmentId ON AssignmentAttachments (AssignmentId);

    IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Assignments')
        ALTER TABLE AssignmentAttachments
            ADD CONSTRAINT FK_AssignmentAttachments_Assignments
            FOREIGN KEY (AssignmentId) REFERENCES Assignments (AssignmentId) ON DELETE CASCADE;

    PRINT 'AssignmentAttachments: table created';
END
ELSE
    PRINT 'AssignmentAttachments: already exists';
