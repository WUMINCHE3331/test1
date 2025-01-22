
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 09/11/2024 21:16:23
-- Generated from EDMX file: C:\Users\mickwu\source\repos\wu2\wu2\Models\Model1.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [wu];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK__ActivityL__Group__38996AB5]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ActivityLogs] DROP CONSTRAINT [FK__ActivityL__Group__38996AB5];
GO
IF OBJECT_ID(N'[dbo].[FK__ActivityL__UserI__398D8EEE]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ActivityLogs] DROP CONSTRAINT [FK__ActivityL__UserI__398D8EEE];
GO
IF OBJECT_ID(N'[dbo].[FK__Debts__CreditorI__3D5E1FD2]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Debts] DROP CONSTRAINT [FK__Debts__CreditorI__3D5E1FD2];
GO
IF OBJECT_ID(N'[dbo].[FK__Debts__DebtorId__3E52440B]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Debts] DROP CONSTRAINT [FK__Debts__DebtorId__3E52440B];
GO
IF OBJECT_ID(N'[dbo].[FK__Debts__GroupId__3C69FB99]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Debts] DROP CONSTRAINT [FK__Debts__GroupId__3C69FB99];
GO
IF OBJECT_ID(N'[dbo].[FK__ExpenseDe__Expen__30F848ED]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ExpenseDetails] DROP CONSTRAINT [FK__ExpenseDe__Expen__30F848ED];
GO
IF OBJECT_ID(N'[dbo].[FK__ExpenseDe__UserI__31EC6D26]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ExpenseDetails] DROP CONSTRAINT [FK__ExpenseDe__UserI__31EC6D26];
GO
IF OBJECT_ID(N'[dbo].[FK__ExpensePa__Expen__4AB81AF0]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ExpensePayers] DROP CONSTRAINT [FK__ExpensePa__Expen__4AB81AF0];
GO
IF OBJECT_ID(N'[dbo].[FK__ExpensePa__UserI__4BAC3F29]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ExpensePayers] DROP CONSTRAINT [FK__ExpensePa__UserI__4BAC3F29];
GO
IF OBJECT_ID(N'[dbo].[FK__Expenses__GroupI__2D27B809]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT [FK__Expenses__GroupI__2D27B809];
GO
IF OBJECT_ID(N'[dbo].[FK__GroupMemb__Group__29572725]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GroupMembers] DROP CONSTRAINT [FK__GroupMemb__Group__29572725];
GO
IF OBJECT_ID(N'[dbo].[FK__GroupMemb__UserI__2A4B4B5E]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GroupMembers] DROP CONSTRAINT [FK__GroupMemb__UserI__2A4B4B5E];
GO
IF OBJECT_ID(N'[dbo].[FK__Groups__CreatorI__267ABA7A]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Groups] DROP CONSTRAINT [FK__Groups__CreatorI__267ABA7A];
GO
IF OBJECT_ID(N'[dbo].[FK__MessageRe__Messa__0C85DE4D]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MessageReadStatus] DROP CONSTRAINT [FK__MessageRe__Messa__0C85DE4D];
GO
IF OBJECT_ID(N'[dbo].[FK__MessageRe__UserI__0D7A0286]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[MessageReadStatus] DROP CONSTRAINT [FK__MessageRe__UserI__0D7A0286];
GO
IF OBJECT_ID(N'[dbo].[FK__Messages__GroupI__07C12930]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Messages] DROP CONSTRAINT [FK__Messages__GroupI__07C12930];
GO
IF OBJECT_ID(N'[dbo].[FK__Messages__UserId__08B54D69]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Messages] DROP CONSTRAINT [FK__Messages__UserId__08B54D69];
GO
IF OBJECT_ID(N'[dbo].[FK__Notices__Created__6EF57B66]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Notices] DROP CONSTRAINT [FK__Notices__Created__6EF57B66];
GO
IF OBJECT_ID(N'[dbo].[FK__Notices__GroupId__6E01572D]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Notices] DROP CONSTRAINT [FK__Notices__GroupId__6E01572D];
GO
IF OBJECT_ID(N'[dbo].[FK__Notificat__Group__35BCFE0A]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT [FK__Notificat__Group__35BCFE0A];
GO
IF OBJECT_ID(N'[dbo].[FK__Notificat__UserI__34C8D9D1]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Notifications] DROP CONSTRAINT [FK__Notificat__UserI__34C8D9D1];
GO
IF OBJECT_ID(N'[dbo].[FK_ActivityLogs_Expenses]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[ActivityLogs] DROP CONSTRAINT [FK_ActivityLogs_Expenses];
GO
IF OBJECT_ID(N'[dbo].[FK_Debts_Expenses]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Debts] DROP CONSTRAINT [FK_Debts_Expenses];
GO
IF OBJECT_ID(N'[dbo].[FK_Expenses_Users]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Expenses] DROP CONSTRAINT [FK_Expenses_Users];
GO
IF OBJECT_ID(N'[dbo].[FK_Messages_Users_ReceiverId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Messages] DROP CONSTRAINT [FK_Messages_Users_ReceiverId];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[ActivityLogs]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ActivityLogs];
GO
IF OBJECT_ID(N'[dbo].[Debts]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Debts];
GO
IF OBJECT_ID(N'[dbo].[ExpenseDetails]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ExpenseDetails];
GO
IF OBJECT_ID(N'[dbo].[ExpensePayers]', 'U') IS NOT NULL
    DROP TABLE [dbo].[ExpensePayers];
GO
IF OBJECT_ID(N'[dbo].[Expenses]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Expenses];
GO
IF OBJECT_ID(N'[dbo].[GroupMembers]', 'U') IS NOT NULL
    DROP TABLE [dbo].[GroupMembers];
GO
IF OBJECT_ID(N'[dbo].[Groups]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Groups];
GO
IF OBJECT_ID(N'[dbo].[MessageReadStatus]', 'U') IS NOT NULL
    DROP TABLE [dbo].[MessageReadStatus];
GO
IF OBJECT_ID(N'[dbo].[Messages]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Messages];
GO
IF OBJECT_ID(N'[dbo].[Notices]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Notices];
GO
IF OBJECT_ID(N'[dbo].[Notifications]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Notifications];
GO
IF OBJECT_ID(N'[dbo].[Users]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Users];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'ActivityLogs'
CREATE TABLE [dbo].[ActivityLogs] (
    [ActivityLogId] int IDENTITY(1,1) NOT NULL,
    [GroupId] int  NULL,
    [UserId] int  NULL,
    [ActivityType] nvarchar(50)  NULL,
    [ActivityDetails] nvarchar(255)  NULL,
    [Date] datetime  NOT NULL,
    [ExpenseId] int  NULL
);
GO

-- Creating table 'Debts'
CREATE TABLE [dbo].[Debts] (
    [DebtId] int IDENTITY(1,1) NOT NULL,
    [GroupId] int  NOT NULL,
    [CreditorId] int  NOT NULL,
    [DebtorId] int  NOT NULL,
    [Amount] decimal(18,2)  NOT NULL,
    [CreatedAt] datetime  NOT NULL,
    [UpdatedAt] datetime  NULL,
    [IsPaid] bit  NOT NULL,
    [ExpenseId] int  NULL,
    [LastRemindTime] datetime  NULL,
    [IsPending] bit  NULL,
    [IsConfirmed] bit  NULL
);
GO

-- Creating table 'ExpenseDetails'
CREATE TABLE [dbo].[ExpenseDetails] (
    [ExpenseDetailId] int IDENTITY(1,1) NOT NULL,
    [ExpenseId] int  NOT NULL,
    [UserId] int  NOT NULL,
    [Amount] decimal(18,2)  NOT NULL,
    [Note] nvarchar(255)  NULL,
    [CreatedAt] datetime  NULL,
    [Proportion] decimal(5,2)  NULL
);
GO

-- Creating table 'ExpensePayers'
CREATE TABLE [dbo].[ExpensePayers] (
    [ExpensePayerId] int IDENTITY(1,1) NOT NULL,
    [ExpenseId] int  NOT NULL,
    [UserId] int  NOT NULL,
    [Amount] decimal(18,2)  NOT NULL
);
GO

-- Creating table 'Expenses'
CREATE TABLE [dbo].[Expenses] (
    [ExpenseId] int IDENTITY(1,1) NOT NULL,
    [GroupId] int  NOT NULL,
    [TotalAmount] decimal(18,2)  NOT NULL,
    [ExpenseType] nvarchar(255)  NOT NULL,
    [ExpenseItem] nvarchar(255)  NOT NULL,
    [PaymentMethod] nvarchar(50)  NOT NULL,
    [Note] nvarchar(255)  NULL,
    [Photo] nvarchar(255)  NULL,
    [CreatedAt] datetime  NULL,
    [CreatedBy] int  NULL,
    [LastTime] datetime  NULL
);
GO

-- Creating table 'GroupMembers'
CREATE TABLE [dbo].[GroupMembers] (
    [GroupMemberId] int IDENTITY(1,1) NOT NULL,
    [GroupId] int  NOT NULL,
    [UserId] int  NOT NULL,
    [Role] nvarchar(50)  NULL,
    [JoinedDate] datetime  NULL
);
GO

-- Creating table 'Groups'
CREATE TABLE [dbo].[Groups] (
    [GroupId] int IDENTITY(1,1) NOT NULL,
    [GroupName] nvarchar(255)  NOT NULL,
    [CreatorId] int  NOT NULL,
    [Budget] decimal(18,2)  NULL,
    [Currency] nvarchar(10)  NOT NULL,
    [CreatedDate] datetime  NULL,
    [Description] nvarchar(255)  NULL,
    [GroupsPhoto] nvarchar(255)  NULL,
    [IsArchived] bit  NOT NULL,
    [JoinLink] nvarchar(255)  NULL,
    [ChatId] nvarchar(50)  NULL
);
GO

-- Creating table 'MessageReadStatus'
CREATE TABLE [dbo].[MessageReadStatus] (
    [MessageReadStatusId] int IDENTITY(1,1) NOT NULL,
    [MessageId] int  NULL,
    [UserId] int  NULL,
    [IsRead] bit  NULL,
    [ReadAt] datetime  NULL
);
GO

-- Creating table 'Messages'
CREATE TABLE [dbo].[Messages] (
    [MessageId] int IDENTITY(1,1) NOT NULL,
    [GroupId] int  NULL,
    [UserId] int  NOT NULL,
    [Content] nvarchar(max)  NULL,
    [CreatedAt] datetime  NOT NULL,
    [ReceiverId] int  NULL
);
GO

-- Creating table 'Notices'
CREATE TABLE [dbo].[Notices] (
    [NoticeId] int IDENTITY(1,1) NOT NULL,
    [GroupId] int  NULL,
    [Title] nvarchar(255)  NULL,
    [Content] varchar(max)  NULL,
    [CreatedAt] datetime  NULL,
    [CreatedBy] int  NULL,
    [IsCurrent] bit  NULL
);
GO

-- Creating table 'Notifications'
CREATE TABLE [dbo].[Notifications] (
    [NotificationId] int IDENTITY(1,1) NOT NULL,
    [UserId] int  NOT NULL,
    [GroupId] int  NULL,
    [NotificationType] nvarchar(50)  NULL,
    [Message] nvarchar(255)  NULL,
    [IsRead] bit  NULL,
    [Date] datetime  NOT NULL,
    [Status] nvarchar(50)  NULL,
    [RelatedDebtId] int  NULL,
    [RelatedExpenseId] int  NULL,
    [IsCancelled] bit  NULL
);
GO

-- Creating table 'Users'
CREATE TABLE [dbo].[Users] (
    [UserId] int IDENTITY(1,1) NOT NULL,
    [Email] nvarchar(255)  NOT NULL,
    [PasswordHash] nvarchar(255)  NOT NULL,
    [FullName] nvarchar(255)  NULL,
    [BankAccount] nvarchar(255)  NULL,
    [PhoneNumber] nvarchar(20)  NULL,
    [Role] nvarchar(50)  NULL,
    [RegistrationDate] datetime  NULL,
    [ProfilePhoto] nvarchar(255)  NULL,
    [TokenExpiration] datetime  NULL,
    [PasswordResetToken] nvarchar(max)  NULL,
    [ConfirmPassword] nvarchar(255)  NULL,
    [LineUserId] nvarchar(50)  NULL,
    [GoogleId] nvarchar(50)  NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [ActivityLogId] in table 'ActivityLogs'
ALTER TABLE [dbo].[ActivityLogs]
ADD CONSTRAINT [PK_ActivityLogs]
    PRIMARY KEY CLUSTERED ([ActivityLogId] ASC);
GO

-- Creating primary key on [DebtId] in table 'Debts'
ALTER TABLE [dbo].[Debts]
ADD CONSTRAINT [PK_Debts]
    PRIMARY KEY CLUSTERED ([DebtId] ASC);
GO

-- Creating primary key on [ExpenseDetailId] in table 'ExpenseDetails'
ALTER TABLE [dbo].[ExpenseDetails]
ADD CONSTRAINT [PK_ExpenseDetails]
    PRIMARY KEY CLUSTERED ([ExpenseDetailId] ASC);
GO

-- Creating primary key on [ExpensePayerId] in table 'ExpensePayers'
ALTER TABLE [dbo].[ExpensePayers]
ADD CONSTRAINT [PK_ExpensePayers]
    PRIMARY KEY CLUSTERED ([ExpensePayerId] ASC);
GO

-- Creating primary key on [ExpenseId] in table 'Expenses'
ALTER TABLE [dbo].[Expenses]
ADD CONSTRAINT [PK_Expenses]
    PRIMARY KEY CLUSTERED ([ExpenseId] ASC);
GO

-- Creating primary key on [GroupMemberId] in table 'GroupMembers'
ALTER TABLE [dbo].[GroupMembers]
ADD CONSTRAINT [PK_GroupMembers]
    PRIMARY KEY CLUSTERED ([GroupMemberId] ASC);
GO

-- Creating primary key on [GroupId] in table 'Groups'
ALTER TABLE [dbo].[Groups]
ADD CONSTRAINT [PK_Groups]
    PRIMARY KEY CLUSTERED ([GroupId] ASC);
GO

-- Creating primary key on [MessageReadStatusId] in table 'MessageReadStatus'
ALTER TABLE [dbo].[MessageReadStatus]
ADD CONSTRAINT [PK_MessageReadStatus]
    PRIMARY KEY CLUSTERED ([MessageReadStatusId] ASC);
GO

-- Creating primary key on [MessageId] in table 'Messages'
ALTER TABLE [dbo].[Messages]
ADD CONSTRAINT [PK_Messages]
    PRIMARY KEY CLUSTERED ([MessageId] ASC);
GO

-- Creating primary key on [NoticeId] in table 'Notices'
ALTER TABLE [dbo].[Notices]
ADD CONSTRAINT [PK_Notices]
    PRIMARY KEY CLUSTERED ([NoticeId] ASC);
GO

-- Creating primary key on [NotificationId] in table 'Notifications'
ALTER TABLE [dbo].[Notifications]
ADD CONSTRAINT [PK_Notifications]
    PRIMARY KEY CLUSTERED ([NotificationId] ASC);
GO

-- Creating primary key on [UserId] in table 'Users'
ALTER TABLE [dbo].[Users]
ADD CONSTRAINT [PK_Users]
    PRIMARY KEY CLUSTERED ([UserId] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [GroupId] in table 'ActivityLogs'
ALTER TABLE [dbo].[ActivityLogs]
ADD CONSTRAINT [FK__ActivityL__Group__38996AB5]
    FOREIGN KEY ([GroupId])
    REFERENCES [dbo].[Groups]
        ([GroupId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__ActivityL__Group__38996AB5'
CREATE INDEX [IX_FK__ActivityL__Group__38996AB5]
ON [dbo].[ActivityLogs]
    ([GroupId]);
GO

-- Creating foreign key on [UserId] in table 'ActivityLogs'
ALTER TABLE [dbo].[ActivityLogs]
ADD CONSTRAINT [FK__ActivityL__UserI__398D8EEE]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__ActivityL__UserI__398D8EEE'
CREATE INDEX [IX_FK__ActivityL__UserI__398D8EEE]
ON [dbo].[ActivityLogs]
    ([UserId]);
GO

-- Creating foreign key on [ExpenseId] in table 'ActivityLogs'
ALTER TABLE [dbo].[ActivityLogs]
ADD CONSTRAINT [FK_ActivityLogs_Expenses]
    FOREIGN KEY ([ExpenseId])
    REFERENCES [dbo].[Expenses]
        ([ExpenseId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_ActivityLogs_Expenses'
CREATE INDEX [IX_FK_ActivityLogs_Expenses]
ON [dbo].[ActivityLogs]
    ([ExpenseId]);
GO

-- Creating foreign key on [CreditorId] in table 'Debts'
ALTER TABLE [dbo].[Debts]
ADD CONSTRAINT [FK__Debts__CreditorI__3D5E1FD2]
    FOREIGN KEY ([CreditorId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Debts__CreditorI__3D5E1FD2'
CREATE INDEX [IX_FK__Debts__CreditorI__3D5E1FD2]
ON [dbo].[Debts]
    ([CreditorId]);
GO

-- Creating foreign key on [DebtorId] in table 'Debts'
ALTER TABLE [dbo].[Debts]
ADD CONSTRAINT [FK__Debts__DebtorId__3E52440B]
    FOREIGN KEY ([DebtorId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Debts__DebtorId__3E52440B'
CREATE INDEX [IX_FK__Debts__DebtorId__3E52440B]
ON [dbo].[Debts]
    ([DebtorId]);
GO

-- Creating foreign key on [GroupId] in table 'Debts'
ALTER TABLE [dbo].[Debts]
ADD CONSTRAINT [FK__Debts__GroupId__3C69FB99]
    FOREIGN KEY ([GroupId])
    REFERENCES [dbo].[Groups]
        ([GroupId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Debts__GroupId__3C69FB99'
CREATE INDEX [IX_FK__Debts__GroupId__3C69FB99]
ON [dbo].[Debts]
    ([GroupId]);
GO

-- Creating foreign key on [ExpenseId] in table 'Debts'
ALTER TABLE [dbo].[Debts]
ADD CONSTRAINT [FK_Debts_Expenses]
    FOREIGN KEY ([ExpenseId])
    REFERENCES [dbo].[Expenses]
        ([ExpenseId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Debts_Expenses'
CREATE INDEX [IX_FK_Debts_Expenses]
ON [dbo].[Debts]
    ([ExpenseId]);
GO

-- Creating foreign key on [ExpenseId] in table 'ExpenseDetails'
ALTER TABLE [dbo].[ExpenseDetails]
ADD CONSTRAINT [FK__ExpenseDe__Expen__30F848ED]
    FOREIGN KEY ([ExpenseId])
    REFERENCES [dbo].[Expenses]
        ([ExpenseId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__ExpenseDe__Expen__30F848ED'
CREATE INDEX [IX_FK__ExpenseDe__Expen__30F848ED]
ON [dbo].[ExpenseDetails]
    ([ExpenseId]);
GO

-- Creating foreign key on [UserId] in table 'ExpenseDetails'
ALTER TABLE [dbo].[ExpenseDetails]
ADD CONSTRAINT [FK__ExpenseDe__UserI__31EC6D26]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__ExpenseDe__UserI__31EC6D26'
CREATE INDEX [IX_FK__ExpenseDe__UserI__31EC6D26]
ON [dbo].[ExpenseDetails]
    ([UserId]);
GO

-- Creating foreign key on [ExpenseId] in table 'ExpensePayers'
ALTER TABLE [dbo].[ExpensePayers]
ADD CONSTRAINT [FK__ExpensePa__Expen__4AB81AF0]
    FOREIGN KEY ([ExpenseId])
    REFERENCES [dbo].[Expenses]
        ([ExpenseId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__ExpensePa__Expen__4AB81AF0'
CREATE INDEX [IX_FK__ExpensePa__Expen__4AB81AF0]
ON [dbo].[ExpensePayers]
    ([ExpenseId]);
GO

-- Creating foreign key on [UserId] in table 'ExpensePayers'
ALTER TABLE [dbo].[ExpensePayers]
ADD CONSTRAINT [FK__ExpensePa__UserI__4BAC3F29]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__ExpensePa__UserI__4BAC3F29'
CREATE INDEX [IX_FK__ExpensePa__UserI__4BAC3F29]
ON [dbo].[ExpensePayers]
    ([UserId]);
GO

-- Creating foreign key on [GroupId] in table 'Expenses'
ALTER TABLE [dbo].[Expenses]
ADD CONSTRAINT [FK__Expenses__GroupI__2D27B809]
    FOREIGN KEY ([GroupId])
    REFERENCES [dbo].[Groups]
        ([GroupId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Expenses__GroupI__2D27B809'
CREATE INDEX [IX_FK__Expenses__GroupI__2D27B809]
ON [dbo].[Expenses]
    ([GroupId]);
GO

-- Creating foreign key on [CreatedBy] in table 'Expenses'
ALTER TABLE [dbo].[Expenses]
ADD CONSTRAINT [FK_Expenses_Users]
    FOREIGN KEY ([CreatedBy])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Expenses_Users'
CREATE INDEX [IX_FK_Expenses_Users]
ON [dbo].[Expenses]
    ([CreatedBy]);
GO

-- Creating foreign key on [GroupId] in table 'GroupMembers'
ALTER TABLE [dbo].[GroupMembers]
ADD CONSTRAINT [FK__GroupMemb__Group__29572725]
    FOREIGN KEY ([GroupId])
    REFERENCES [dbo].[Groups]
        ([GroupId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__GroupMemb__Group__29572725'
CREATE INDEX [IX_FK__GroupMemb__Group__29572725]
ON [dbo].[GroupMembers]
    ([GroupId]);
GO

-- Creating foreign key on [UserId] in table 'GroupMembers'
ALTER TABLE [dbo].[GroupMembers]
ADD CONSTRAINT [FK__GroupMemb__UserI__2A4B4B5E]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__GroupMemb__UserI__2A4B4B5E'
CREATE INDEX [IX_FK__GroupMemb__UserI__2A4B4B5E]
ON [dbo].[GroupMembers]
    ([UserId]);
GO

-- Creating foreign key on [CreatorId] in table 'Groups'
ALTER TABLE [dbo].[Groups]
ADD CONSTRAINT [FK__Groups__CreatorI__267ABA7A]
    FOREIGN KEY ([CreatorId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Groups__CreatorI__267ABA7A'
CREATE INDEX [IX_FK__Groups__CreatorI__267ABA7A]
ON [dbo].[Groups]
    ([CreatorId]);
GO

-- Creating foreign key on [GroupId] in table 'Messages'
ALTER TABLE [dbo].[Messages]
ADD CONSTRAINT [FK__Messages__GroupI__07C12930]
    FOREIGN KEY ([GroupId])
    REFERENCES [dbo].[Groups]
        ([GroupId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Messages__GroupI__07C12930'
CREATE INDEX [IX_FK__Messages__GroupI__07C12930]
ON [dbo].[Messages]
    ([GroupId]);
GO

-- Creating foreign key on [GroupId] in table 'Notices'
ALTER TABLE [dbo].[Notices]
ADD CONSTRAINT [FK__Notices__GroupId__6E01572D]
    FOREIGN KEY ([GroupId])
    REFERENCES [dbo].[Groups]
        ([GroupId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Notices__GroupId__6E01572D'
CREATE INDEX [IX_FK__Notices__GroupId__6E01572D]
ON [dbo].[Notices]
    ([GroupId]);
GO

-- Creating foreign key on [GroupId] in table 'Notifications'
ALTER TABLE [dbo].[Notifications]
ADD CONSTRAINT [FK__Notificat__Group__35BCFE0A]
    FOREIGN KEY ([GroupId])
    REFERENCES [dbo].[Groups]
        ([GroupId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Notificat__Group__35BCFE0A'
CREATE INDEX [IX_FK__Notificat__Group__35BCFE0A]
ON [dbo].[Notifications]
    ([GroupId]);
GO

-- Creating foreign key on [MessageId] in table 'MessageReadStatus'
ALTER TABLE [dbo].[MessageReadStatus]
ADD CONSTRAINT [FK__MessageRe__Messa__0C85DE4D]
    FOREIGN KEY ([MessageId])
    REFERENCES [dbo].[Messages]
        ([MessageId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__MessageRe__Messa__0C85DE4D'
CREATE INDEX [IX_FK__MessageRe__Messa__0C85DE4D]
ON [dbo].[MessageReadStatus]
    ([MessageId]);
GO

-- Creating foreign key on [UserId] in table 'MessageReadStatus'
ALTER TABLE [dbo].[MessageReadStatus]
ADD CONSTRAINT [FK__MessageRe__UserI__0D7A0286]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__MessageRe__UserI__0D7A0286'
CREATE INDEX [IX_FK__MessageRe__UserI__0D7A0286]
ON [dbo].[MessageReadStatus]
    ([UserId]);
GO

-- Creating foreign key on [UserId] in table 'Messages'
ALTER TABLE [dbo].[Messages]
ADD CONSTRAINT [FK__Messages__UserId__08B54D69]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Messages__UserId__08B54D69'
CREATE INDEX [IX_FK__Messages__UserId__08B54D69]
ON [dbo].[Messages]
    ([UserId]);
GO

-- Creating foreign key on [ReceiverId] in table 'Messages'
ALTER TABLE [dbo].[Messages]
ADD CONSTRAINT [FK_Messages_Users_ReceiverId]
    FOREIGN KEY ([ReceiverId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Messages_Users_ReceiverId'
CREATE INDEX [IX_FK_Messages_Users_ReceiverId]
ON [dbo].[Messages]
    ([ReceiverId]);
GO

-- Creating foreign key on [CreatedBy] in table 'Notices'
ALTER TABLE [dbo].[Notices]
ADD CONSTRAINT [FK__Notices__Created__6EF57B66]
    FOREIGN KEY ([CreatedBy])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Notices__Created__6EF57B66'
CREATE INDEX [IX_FK__Notices__Created__6EF57B66]
ON [dbo].[Notices]
    ([CreatedBy]);
GO

-- Creating foreign key on [UserId] in table 'Notifications'
ALTER TABLE [dbo].[Notifications]
ADD CONSTRAINT [FK__Notificat__UserI__34C8D9D1]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[Users]
        ([UserId])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK__Notificat__UserI__34C8D9D1'
CREATE INDEX [IX_FK__Notificat__UserI__34C8D9D1]
ON [dbo].[Notifications]
    ([UserId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------