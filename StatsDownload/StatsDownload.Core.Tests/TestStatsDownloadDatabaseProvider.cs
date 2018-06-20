﻿namespace StatsDownload.Core.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using Implementations;
    using Interfaces;
    using Interfaces.DataTransfer;
    using Interfaces.Enums;
    using Interfaces.Logging;
    using NSubstitute;
    using NSubstitute.ClearExtensions;
    using NUnit.Framework;

    [TestFixture]
    public class TestStatsDownloadDatabaseProvider
    {
        [SetUp]
        public void SetUp()
        {
            databaseConnectionSettingsServiceMock = Substitute.For<IDatabaseConnectionSettingsService>();
            databaseConnectionSettingsServiceMock.GetConnectionString().Returns("connectionString");
            databaseConnectionSettingsServiceMock.GetCommandTimeout().Returns(42);

            databaseConnectionServiceMock = Substitute.For<IDatabaseConnectionService>();
            databaseConnectionServiceFactoryMock = Substitute.For<IDatabaseConnectionServiceFactory>();
            databaseConnectionServiceFactoryMock.Create("connectionString", 42).Returns(databaseConnectionServiceMock);

            loggingServiceMock = Substitute.For<ILoggingService>();

            errorMessageServiceMock = Substitute.For<IErrorMessageService>();

            systemUnderTest = NewFileDownloadDataStoreProvider(databaseConnectionSettingsServiceMock,
                databaseConnectionServiceFactoryMock, loggingServiceMock, errorMessageServiceMock);

            databaseConnectionServiceMock.CreateParameter(Arg.Any<string>(), Arg.Any<DbType>(),
                Arg.Any<ParameterDirection>()).Returns(info =>
            {
                var parameterName = info.Arg<string>();
                var dbType = info.Arg<DbType>();
                var direction = info.Arg<ParameterDirection>();

                var dbParameter = Substitute.For<DbParameter>();
                dbParameter.ParameterName.Returns(parameterName);
                dbParameter.DbType.Returns(dbType);
                dbParameter.Direction.Returns(direction);

                if (dbType.Equals(DbType.Int32))
                {
                    dbParameter.Value.Returns(default(int));
                }

                return dbParameter;
            });

            databaseConnectionServiceMock.CreateParameter(Arg.Any<string>(), Arg.Any<DbType>(),
                Arg.Any<ParameterDirection>(), Arg.Any<int>()).Returns(info =>
            {
                var parameterName = info.Arg<string>();
                var dbType = info.Arg<DbType>();
                var direction = info.Arg<ParameterDirection>();
                var size = info.Arg<int>();

                var dbParameter = Substitute.For<DbParameter>();
                dbParameter.ParameterName.Returns(parameterName);
                dbParameter.DbType.Returns(dbType);
                dbParameter.Direction.Returns(direction);
                dbParameter.Size.Returns(size);

                if (dbType.Equals(DbType.Int32))
                {
                    dbParameter.Value.Returns(default(int));
                }

                return dbParameter;
            });

            filePayload = new FilePayload
            {
                DownloadId = 100,
                DecompressedDownloadFileName = "DecompressedDownloadFileName",
                DecompressedDownloadFileExtension = "DecompressedDownloadFileExtension",
                DecompressedDownloadFileData = "DecompressedDownloadFileData"
            };

            fileDownloadResult = new FileDownloadResult(filePayload);

            dbDataReaderMock = Substitute.For<DbDataReader>();
            dbDataReaderMock.Read().Returns(true, true, true, false);
            dbDataReaderMock.GetInt32(0).Returns(100, 200, 300);

            databaseConnectionServiceMock
                .ExecuteReader("SELECT DownloadId FROM [FoldingCoin].[DownloadsReadyForUpload]")
                .Returns(dbDataReaderMock);
        }

        private IDatabaseConnectionServiceFactory databaseConnectionServiceFactoryMock;

        private IDatabaseConnectionService databaseConnectionServiceMock;

        private IDatabaseConnectionSettingsService databaseConnectionSettingsServiceMock;

        private DbDataReader dbDataReaderMock;

        private IErrorMessageService errorMessageServiceMock;

        private FileDownloadResult fileDownloadResult;

        private FilePayload filePayload;

        private ILoggingService loggingServiceMock;

        private const int NumberOfRowsEffectedExpected = 5;

        private IStatsDownloadDatabaseService systemUnderTest;

        [Test]
        public void AddUsers_WhenInvoked_AddsUsers()
        {
            DbCommand failedUsersCommand = null;
            DbCommand addUsersCommand = null;
            DbCommand rebuildIndicesCommand = null;
            SetUpDatabaseConnectionCreateDbCommandMock(new Action<DbCommand>[]
            {
                dbCommand => failedUsersCommand = dbCommand,
                dbCommand => addUsersCommand = dbCommand,
                dbCommand => rebuildIndicesCommand = dbCommand
            });

            systemUnderTest.AddUsers(null, 1, new List<UserData> { new UserData(), new UserData(), new UserData() },
                new List<FailedUserData> { new FailedUserData(), new FailedUserData() });

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("AddUsers Invoked");
                loggingServiceMock.LogVerbose("Database connection was successful");
                failedUsersCommand.ExecuteNonQuery();
                failedUsersCommand.ExecuteNonQuery();
                addUsersCommand.ExecuteNonQuery();
                rebuildIndicesCommand.ExecuteNonQuery();
                addUsersCommand.ExecuteNonQuery();
                addUsersCommand.ExecuteNonQuery();
            });
        }

        [Test]
        public void AddUsers_WhenInvoked_AddUserDataParametersAreProvided()
        {
            List<DbParameter> actualParameters = default(List<DbParameter>);

            SetUpDatabaseConnectionCreateDbCommandMock(null,
                new Action<List<DbParameter>>[] { null, parameters => { actualParameters = parameters; } });

            systemUnderTest.AddUsers(null, 1,
                new List<UserData>
                {
                    new UserData(999, "name", 10, 100, 1000)
                    {
                        BitcoinAddress = "address",
                        FriendlyName = "friendly"
                    }
                }, null);

            Assert.That(actualParameters.Count, Is.EqualTo(8));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(1));
            Assert.That(actualParameters[1].ParameterName, Is.EqualTo("@LineNumber"));
            Assert.That(actualParameters[1].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[1].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[1].Value, Is.EqualTo(999));
            Assert.That(actualParameters[2].ParameterName, Is.EqualTo("@FAHUserName"));
            Assert.That(actualParameters[2].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[2].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[2].Value, Is.EqualTo("name"));
            Assert.That(actualParameters[3].ParameterName, Is.EqualTo("@TotalPoints"));
            Assert.That(actualParameters[3].DbType, Is.EqualTo(DbType.Int64));
            Assert.That(actualParameters[3].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[3].Value, Is.EqualTo(10));
            Assert.That(actualParameters[4].ParameterName, Is.EqualTo("@WorkUnits"));
            Assert.That(actualParameters[4].DbType, Is.EqualTo(DbType.Int64));
            Assert.That(actualParameters[4].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[4].Value, Is.EqualTo(100));
            Assert.That(actualParameters[5].ParameterName, Is.EqualTo("@TeamNumber"));
            Assert.That(actualParameters[5].DbType, Is.EqualTo(DbType.Int64));
            Assert.That(actualParameters[5].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[5].Value, Is.EqualTo(1000));
            Assert.That(actualParameters[6].ParameterName, Is.EqualTo("@FriendlyName"));
            Assert.That(actualParameters[6].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[6].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[6].Value, Is.EqualTo("friendly"));
            Assert.That(actualParameters[7].ParameterName, Is.EqualTo("@BitcoinAddress"));
            Assert.That(actualParameters[7].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[7].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[7].Value, Is.EqualTo("address"));
        }

        [Test]
        public void AddUsers_WhenInvoked_AddUserRejectionParametersAreProvided()
        {
            var failedUserData = new FailedUserData(10, "", RejectionReason.UnexpectedFormat, new UserData());
            errorMessageServiceMock.GetErrorMessage(failedUserData).Returns("RejectionReason");

            List<DbParameter> actualParameters = default(List<DbParameter>);
            SetUpDatabaseConnectionCreateDbCommandMock(null,
                new Action<List<DbParameter>>[] { parameters => { actualParameters = parameters; } });

            systemUnderTest.AddUsers(null, 1, null, new[] { failedUserData });

            Assert.That(actualParameters.Count, Is.EqualTo(3));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(1));
            Assert.That(actualParameters[1].ParameterName, Is.EqualTo("@LineNumber"));
            Assert.That(actualParameters[1].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[1].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[1].Value, Is.EqualTo(10));
            Assert.That(actualParameters[2].ParameterName, Is.EqualTo("@RejectionReason"));
            Assert.That(actualParameters[2].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[2].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[2].Value, Is.EqualTo("RejectionReason"));
        }

        [Test]
        public void AddUsers_WhenInvoked_DisposesCommands()
        {
            DbCommand failedUsersCommand = null;
            DbCommand addUsersCommand = null;
            DbCommand rebuildIndicesCommand = null;
            SetUpDatabaseConnectionCreateDbCommandMock(new Action<DbCommand>[]
            {
                dbCommand => failedUsersCommand = dbCommand,
                dbCommand => addUsersCommand = dbCommand,
                dbCommand => rebuildIndicesCommand = dbCommand
            });

            systemUnderTest.AddUsers(null, 1, new[] { new UserData(), new UserData() }, null);

            failedUsersCommand.Received(1).Dispose();
            addUsersCommand.Received(1).Dispose();
            rebuildIndicesCommand.Received(1).Dispose();
        }

        [Test]
        public void AddUsers_WhenInvoked_RebuildsIndicesPeriodically()
        {
            DbCommand command = null;
            SetUpDatabaseConnectionCreateDbCommandMock(new Action<DbCommand>[]
            {
                null,
                null,
                dbCommand => command = dbCommand
            });

            var users = new UserData[2501];
            for (var index = 0; index < users.Length; index++)
            {
                users[index] = new UserData();
            }

            systemUnderTest.AddUsers(null, 1, users, null);

            command.Received(2).ExecuteNonQuery();
        }

        [Test]
        public void AddUsers_WhenInvoked_ReusesCommands()
        {
            SetUpDatabaseConnectionCreateDbCommandMock();

            systemUnderTest.AddUsers(null, 0, null, null);

            databaseConnectionServiceMock.Received(3).CreateDbCommand();
        }

        [Test]
        public void AddUsers_WhenInvoked_ReusesParameters()
        {
            SetUpDatabaseConnectionCreateDbCommandMock();

            systemUnderTest.AddUsers(null, 0, null, null);

            databaseConnectionServiceMock.ReceivedWithAnyArgs(11)
                                         .CreateParameter(null, DbType.AnsiString, ParameterDirection.Input);
        }

        [Test]
        public void AddUsers_WhenInvoked_UsesAddUserDataStoredProcedure()
        {
            DbCommand command = default(DbCommand);
            SetUpDatabaseConnectionCreateDbCommandMock(new Action<DbCommand>[]
            {
                null,
                dbCommand => command = dbCommand
            });

            var transactionMock = Substitute.For<DbTransaction>();

            systemUnderTest.AddUsers(transactionMock, 1, null, null);

            command.Received(1).CommandText = "[FoldingCoin].[AddUserData]";
            command.Received(1).CommandType = CommandType.StoredProcedure;
            command.Received(1).Transaction = transactionMock;
        }

        [Test]
        public void AddUsers_WhenInvoked_UsesAddUserRejectionStoredProcedure()
        {
            DbCommand command = default(DbCommand);
            SetUpDatabaseConnectionCreateDbCommandMock(new Action<DbCommand>[] { dbCommand => command = dbCommand });

            var transactionMock = Substitute.For<DbTransaction>();

            systemUnderTest.AddUsers(transactionMock, 1, null, null);

            command.Received(1).CommandText = "[FoldingCoin].[AddUserRejection]";
            command.Received(1).CommandType = CommandType.StoredProcedure;
            command.Received(1).Transaction = transactionMock;
        }

        [Test]
        public void AddUsers_WhenInvoked_UsesRebuildsIndicesStoredProcedure()
        {
            DbCommand command = default(DbCommand);
            SetUpDatabaseConnectionCreateDbCommandMock(new Action<DbCommand>[]
            {
                null,
                null,
                dbCommand => command = dbCommand
            });

            var transactionMock = Substitute.For<DbTransaction>();

            systemUnderTest.AddUsers(transactionMock, 1, null, null);

            command.Received(1).CommandText = "[FoldingCoin].[RebuildIndices]";
            command.Received(1).CommandType = CommandType.StoredProcedure;
            command.Received(1).Transaction = transactionMock;
        }

        [Test]
        public void AddUsers_WhenInvokedWithNullBitcoinAddress_ParameterIsDBNull()
        {
            List<DbParameter> actualParameters = default(List<DbParameter>);
            SetUpDatabaseConnectionCreateDbCommandMock(null,
                new Action<List<DbParameter>>[] { null, parameters => { actualParameters = parameters; } });

            systemUnderTest.AddUsers(null, 1,
                new List<UserData> { new UserData(0, "name", 10, 100, 1000) { FriendlyName = "friendly" } }, null);

            Assert.That(actualParameters.Count, Is.EqualTo(8));
            Assert.That(actualParameters[7].ParameterName, Is.EqualTo("@BitcoinAddress"));
            Assert.That(actualParameters[7].Value, Is.EqualTo(DBNull.Value));
        }

        [Test]
        public void AddUsers_WhenInvokedWithNullFriendlyName_ParameterIsDBNull()
        {
            List<DbParameter> actualParameters = default(List<DbParameter>);
            SetUpDatabaseConnectionCreateDbCommandMock(null,
                new Action<List<DbParameter>>[] { null, parameters => { actualParameters = parameters; } });

            systemUnderTest.AddUsers(null, 1,
                new List<UserData> { new UserData(0, "name", 10, 100, 1000) { BitcoinAddress = "address" } }, null);

            Assert.That(actualParameters.Count, Is.AtLeast(8));
            Assert.That(actualParameters[6].ParameterName, Is.EqualTo("@FriendlyName"));
            Assert.That(actualParameters[6].Value, Is.EqualTo(DBNull.Value));
        }

        [Test]
        public void Commit_WhenInvoked_CommitsTransaction()
        {
            var transaction = Substitute.For<DbTransaction>();

            systemUnderTest.Commit(transaction);

            transaction.Received(1).Commit();
        }

        [Test]
        public void Constructor_WhenNullDependencyProvided_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(
                () =>
                    NewFileDownloadDataStoreProvider(null, databaseConnectionServiceFactoryMock, loggingServiceMock,
                        errorMessageServiceMock));
            Assert.Throws<ArgumentNullException>(
                () =>
                    NewFileDownloadDataStoreProvider(databaseConnectionSettingsServiceMock, null, loggingServiceMock,
                        errorMessageServiceMock));
            Assert.Throws<ArgumentNullException>(
                () =>
                    NewFileDownloadDataStoreProvider(databaseConnectionSettingsServiceMock,
                        databaseConnectionServiceFactoryMock, null, errorMessageServiceMock));
            Assert.Throws<ArgumentNullException>(
                () =>
                    NewFileDownloadDataStoreProvider(databaseConnectionSettingsServiceMock,
                        databaseConnectionServiceFactoryMock, loggingServiceMock, null));
        }

        [Test]
        public void FileDownloadError_WhenInvoked_ParametersAreProvided()
        {
            errorMessageServiceMock.GetErrorMessage(FailedReason.UnexpectedException, filePayload)
                                   .Returns("ErrorMessage");
            fileDownloadResult = new FileDownloadResult(FailedReason.UnexpectedException, filePayload);

            List<DbParameter> actualParameters = default(List<DbParameter>);

            databaseConnectionServiceMock.When(
                                             service =>
                                                 service.ExecuteStoredProcedure("[FoldingCoin].[FileDownloadError]",
                                                     Arg.Any<List<DbParameter>>()))
                                         .Do(callback => { actualParameters = callback.Arg<List<DbParameter>>(); });

            InvokeFileDownloadError();

            Assert.That(actualParameters.Count, Is.EqualTo(2));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(100));
            Assert.That(actualParameters[1].ParameterName, Is.EqualTo("@ErrorMessage"));
            Assert.That(actualParameters[1].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[1].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[1].Value, Is.EqualTo("ErrorMessage"));
        }

        [Test]
        public void FileDownloadError_WhenInvoked_UpdatesFileDownloadToError()
        {
            InvokeFileDownloadError();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("FileDownloadError Invoked");
                loggingServiceMock.LogVerbose("Database connection was successful");
                databaseConnectionServiceMock.ExecuteStoredProcedure("[FoldingCoin].[FileDownloadError]",
                    Arg.Any<List<DbParameter>>());
            });
        }

        [Test]
        public void FileDownloadFinished_WhenInvoked_FileDataUpload()
        {
            InvokeFileDownloadFinished();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("FileDownloadFinished Invoked");
                databaseConnectionServiceMock.ExecuteStoredProcedure("[FoldingCoin].[FileDownloadFinished]",
                    Arg.Any<List<DbParameter>>());
            });
        }

        [Test]
        public void FileDownloadFinished_WhenInvoked_ParametersAreProvided()
        {
            List<DbParameter> actualParameters = default(List<DbParameter>);

            databaseConnectionServiceMock.When(
                                             service =>
                                                 service.ExecuteStoredProcedure("[FoldingCoin].[FileDownloadFinished]",
                                                     Arg.Any<List<DbParameter>>()))
                                         .Do(callback => { actualParameters = callback.Arg<List<DbParameter>>(); });

            InvokeFileDownloadFinished();

            Assert.That(actualParameters.Count, Is.EqualTo(4));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(100));
            Assert.That(actualParameters[1].ParameterName, Is.EqualTo("@FileName"));
            Assert.That(actualParameters[1].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[1].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[1].Value, Is.EqualTo("DecompressedDownloadFileName"));
            Assert.That(actualParameters[2].ParameterName, Is.EqualTo("@FileExtension"));
            Assert.That(actualParameters[2].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[2].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[2].Value, Is.EqualTo("DecompressedDownloadFileExtension"));
            Assert.That(actualParameters[3].ParameterName, Is.EqualTo("@FileData"));
            Assert.That(actualParameters[3].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[3].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[3].Value, Is.EqualTo("DecompressedDownloadFileData"));
        }

        [Test]
        public void GetDownloadsReadyForUpload_WhenInvoked_DisposesReader()
        {
            systemUnderTest.GetDownloadsReadyForUpload();

            dbDataReaderMock.Received().Dispose();
        }

        [Test]
        public void GetDownloadsReadyForUpload_WhenInvoked_GetsDownloadsReadyForUpload()
        {
            systemUnderTest.GetDownloadsReadyForUpload();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("GetDownloadsReadyForUpload Invoked");
                databaseConnectionServiceMock.ExecuteReader(
                    "SELECT DownloadId FROM [FoldingCoin].[DownloadsReadyForUpload]");
            });
        }

        [Test]
        public void GetDownloadsReadyForUpload_WhenInvoked_ReturnsDownloadIds()
        {
            List<int> actual = systemUnderTest.GetDownloadsReadyForUpload().ToList();

            Assert.That(actual.Count, Is.EqualTo(3));
            Assert.That(actual[1], Is.EqualTo(200));
        }

        [Test]
        public void GetDownloadsReadyForUpload_WhenNoFilesReadyForUpload_ReturnsEmptyList()
        {
            dbDataReaderMock.ClearSubstitute();
            dbDataReaderMock.Read().Returns(false);

            List<int> actual = systemUnderTest.GetDownloadsReadyForUpload().ToList();

            Assert.That(actual.Count, Is.EqualTo(0));
        }

        [Test]
        public void GetFileData_WhenInvoked_GetFileData()
        {
            systemUnderTest.GetFileData(100);

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("GetFileData Invoked");
                databaseConnectionServiceMock.ExecuteStoredProcedure("[FoldingCoin].[GetFileData]",
                    Arg.Any<List<DbParameter>>());
            });
        }

        [Test]
        public void GetFileData_WhenInvoked_ParametersAreProvided()
        {
            List<DbParameter> actualParameters = default(List<DbParameter>);

            databaseConnectionServiceMock.When(
                                             service => service.ExecuteStoredProcedure("[FoldingCoin].[GetFileData]",
                                                 Arg.Any<List<DbParameter>>()))
                                         .Do(callback => { actualParameters = callback.Arg<List<DbParameter>>(); });

            systemUnderTest.GetFileData(100);

            Assert.That(actualParameters.Count, Is.EqualTo(4));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(100));
            Assert.That(actualParameters[1].ParameterName, Is.EqualTo("@FileName"));
            Assert.That(actualParameters[1].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[1].Direction, Is.EqualTo(ParameterDirection.Output));
            Assert.That(actualParameters[1].Size, Is.EqualTo(-1));
            Assert.That(actualParameters[2].ParameterName, Is.EqualTo("@FileExtension"));
            Assert.That(actualParameters[2].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[2].Direction, Is.EqualTo(ParameterDirection.Output));
            Assert.That(actualParameters[2].Size, Is.EqualTo(-1));
            Assert.That(actualParameters[3].ParameterName, Is.EqualTo("@FileData"));
            Assert.That(actualParameters[3].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[3].Direction, Is.EqualTo(ParameterDirection.Output));
            Assert.That(actualParameters[3].Size, Is.EqualTo(-1));
        }

        [Test]
        public void GetFileData_WhenInvoked_ReturnsFileData()
        {
            var dbParameter = Substitute.For<DbParameter>();
            dbParameter.Value.Returns("FileData");

            databaseConnectionServiceMock.ClearSubstitute();
            databaseConnectionServiceMock.CreateParameter("@FileData", DbType.String, ParameterDirection.Output, -1)
                                         .Returns(dbParameter);
            databaseConnectionServiceMock.CreateParameter("@DownloadId", DbType.Int32, ParameterDirection.Input)
                                         .Returns(Substitute.For<DbParameter>());

            string actual = systemUnderTest.GetFileData(100);

            Assert.That(actual, Is.EqualTo("FileData"));
        }

        [Test]
        public void GetLastFileDownloadDateTime_WhenInvoked_GetsLastfileDownloadDateTime()
        {
            InvokeGetLastFileFownloadDateTime();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("GetLastFileDownloadDateTime Invoked");
                databaseConnectionServiceMock.ExecuteScalar("SELECT [FoldingCoin].[GetLastFileDownloadDateTime]()");
            });
        }

        [Test]
        public void GetLastFileDownloadDateTime_WhenInvoked_ReturnsDateTime()
        {
            DateTime dateTime = DateTime.Now;
            databaseConnectionServiceMock.ExecuteScalar("SELECT [FoldingCoin].[GetLastFileDownloadDateTime]()")
                                         .Returns(dateTime);

            DateTime actual = InvokeGetLastFileFownloadDateTime();

            Assert.That(actual, Is.EqualTo(dateTime));
        }

        [Test]
        public void GetLastFileDownloadDateTime_WhenNoRowsReturned_ReturnsDefaultDateTime()
        {
            DateTime actual = InvokeGetLastFileFownloadDateTime();

            Assert.That(actual, Is.EqualTo(default(DateTime)));
        }

        [Test]
        public void IsAvailable_WhenConnectionClosed_ConnectionOpened()
        {
            databaseConnectionServiceMock.ConnectionState.Returns(ConnectionState.Closed);

            InvokeIsAvailable();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("IsAvailable Invoked");
                databaseConnectionServiceMock.Open();
                loggingServiceMock.LogVerbose("Database connection was successful");
            });
        }

        [Test]
        public void IsAvailable_WhenConnectionOpen_ConnectionNotOpened()
        {
            databaseConnectionServiceMock.ConnectionState.Returns(ConnectionState.Open);

            InvokeIsAvailable();

            loggingServiceMock.DidNotReceive().LogVerbose("Database connection was successful");
            databaseConnectionServiceMock.DidNotReceive().Open();
        }

        [Test]
        public void IsAvailable_WhenDatabaseConnectionFails_LogsException()
        {
            var expected = new Exception();
            databaseConnectionServiceMock.When(mock => mock.Open()).Throw(expected);

            InvokeIsAvailable();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("IsAvailable Invoked");
                databaseConnectionServiceMock.Open();
                loggingServiceMock.LogException(expected);
            });
        }

        [Test]
        public void IsAvailable_WhenDatabaseConnectionFails_ReturnsFalse()
        {
            databaseConnectionServiceMock.When(mock => mock.Open()).Throw<Exception>();

            bool actual = InvokeIsAvailable();

            Assert.That(actual, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        public void IsAvailable_WhenInvalidConnectionString_ReturnsFalse(string connectionString)
        {
            databaseConnectionSettingsServiceMock.GetConnectionString().Returns(connectionString);

            bool actual = InvokeIsAvailable();

            Assert.That(actual, Is.False);
        }

        [Test]
        public void IsAvailable_WhenInvoked_ReturnsTrue()
        {
            bool actual = InvokeIsAvailable();

            Assert.That(actual, Is.True);
        }

        [Test]
        public void NewFileDownloadStarted_WhenEmptyString_ThrowsArgumentException()
        {
            databaseConnectionSettingsServiceMock.GetConnectionString().Returns(string.Empty);

            Assert.Throws<ArgumentException>(InvokeNewFileDownloadStarted);
        }

        [Test]
        public void NewFileDownloadStarted_WhenInvoked_NewFileDownloadStarted()
        {
            InvokeNewFileDownloadStarted();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("NewFileDownloadStarted Invoked");
                loggingServiceMock.LogVerbose("Database connection was successful");
                databaseConnectionServiceMock.ExecuteStoredProcedure("[FoldingCoin].[NewFileDownloadStarted]",
                    Arg.Any<List<DbParameter>>());
            });
        }

        [Test]
        public void NewFileDownloadStarted_WhenInvoked_ParametersAreProvided()
        {
            List<DbParameter> actualParameters = default(List<DbParameter>);

            databaseConnectionServiceMock.When(
                                             service =>
                                                 service.ExecuteStoredProcedure(
                                                     "[FoldingCoin].[NewFileDownloadStarted]",
                                                     Arg.Any<List<DbParameter>>()))
                                         .Do(callback => { actualParameters = callback.Arg<List<DbParameter>>(); });

            InvokeNewFileDownloadStarted();

            Assert.That(actualParameters.Count, Is.EqualTo(1));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Output));
        }

        [Test]
        public void NewFileDownloadStarted_WhenInvoked_ReturnsDownloadId()
        {
            var dbParameter = Substitute.For<DbParameter>();
            dbParameter.Value.Returns(101);

            databaseConnectionServiceMock.ClearSubstitute();
            databaseConnectionServiceMock.CreateParameter("@DownloadId", DbType.Int32, ParameterDirection.Output)
                                         .Returns(dbParameter);

            InvokeNewFileDownloadStarted();

            Assert.That(filePayload.DownloadId, Is.EqualTo(101));
        }

        [Test]
        public void NewFileDownloadStarted_WhenNullConnectionString_ThrowsArgumentNullException()
        {
            databaseConnectionSettingsServiceMock.GetConnectionString().Returns((string) null);

            Assert.Throws<ArgumentNullException>(InvokeNewFileDownloadStarted);
        }

        [Test]
        public void Rollback_WhenInvoked_RollsBackTransaction()
        {
            var transaction = Substitute.For<DbTransaction>();

            systemUnderTest.Rollback(transaction);

            transaction.Received(1).Rollback();
        }

        [Test]
        public void StartStatsUpload_WhenInvoked_ParameterIsProvided()
        {
            var transaction = Substitute.For<DbTransaction>();
            databaseConnectionServiceMock.CreateTransaction().Returns(transaction);

            List<DbParameter> actualParameters = default(List<DbParameter>);

            databaseConnectionServiceMock.When(
                                             service =>
                                                 service.ExecuteStoredProcedure(transaction,
                                                     "[FoldingCoin].[StartStatsUpload]",
                                                     Arg.Any<List<DbParameter>>()))
                                         .Do(callback => { actualParameters = callback.Arg<List<DbParameter>>(); });

            DateTime dateTime = DateTime.UtcNow;

            systemUnderTest.StartStatsUpload(100, dateTime);

            Assert.That(actualParameters.Count, Is.EqualTo(2));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(100));
            Assert.That(actualParameters[1].ParameterName, Is.EqualTo("@DownloadDateTime"));
            Assert.That(actualParameters[1].DbType, Is.EqualTo(DbType.DateTime));
            Assert.That(actualParameters[1].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[1].Value, Is.EqualTo(dateTime));
        }

        [Test]
        public void StartStatsUpload_WhenInvoked_ReturnsTransaction()
        {
            var expected = Substitute.For<DbTransaction>();
            databaseConnectionServiceMock.CreateTransaction().Returns(expected);

            DbTransaction actual = systemUnderTest.StartStatsUpload(100, DateTime.Now);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void StartStatsUpload_WhenInvoked_StartsStatsUpload()
        {
            var transaction = Substitute.For<DbTransaction>();
            databaseConnectionServiceMock.CreateTransaction().Returns(transaction);

            systemUnderTest.StartStatsUpload(1, DateTime.Now);

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("StartStatsUpload Invoked");
                loggingServiceMock.LogVerbose("Database connection was successful");
                databaseConnectionServiceMock.ExecuteStoredProcedure(transaction, "[FoldingCoin].[StartStatsUpload]",
                    Arg.Any<List<DbParameter>>());
            });
        }

        [Test]
        public void StatsUploadError_WhenInvoked_ParametersAreProvided()
        {
            errorMessageServiceMock.GetErrorMessage(FailedReason.UnexpectedException).Returns("ErrorMessage");

            List<DbParameter> actualParameters = default(List<DbParameter>);

            databaseConnectionServiceMock.When(
                                             service =>
                                                 service.ExecuteStoredProcedure("[FoldingCoin].[StatsUploadError]",
                                                     Arg.Any<List<DbParameter>>()))
                                         .Do(callback => { actualParameters = callback.Arg<List<DbParameter>>(); });

            systemUnderTest.StatsUploadError(new StatsUploadResult(100, FailedReason.UnexpectedException));

            Assert.That(actualParameters.Count, Is.EqualTo(2));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(100));
            Assert.That(actualParameters[1].ParameterName, Is.EqualTo("@ErrorMessage"));
            Assert.That(actualParameters[1].DbType, Is.EqualTo(DbType.String));
            Assert.That(actualParameters[1].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[1].Value, Is.EqualTo("ErrorMessage"));
        }

        [Test]
        public void StatsUploadError_WhenInvoked_UpdatesStatsUploadToError()
        {
            systemUnderTest.StatsUploadError(new StatsUploadResult());

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("StatsUploadError Invoked");
                databaseConnectionServiceMock.ExecuteStoredProcedure("[FoldingCoin].[StatsUploadError]",
                    Arg.Any<List<DbParameter>>());
            });
        }

        [Test]
        public void StatsUploadFinished_WhenInvoked_ParametersAreProvided()
        {
            var transaction = Substitute.For<DbTransaction>();

            List<DbParameter> actualParameters = default(List<DbParameter>);

            databaseConnectionServiceMock.When(
                                             service =>
                                                 service.ExecuteStoredProcedure(transaction,
                                                     "[FoldingCoin].[StatsUploadFinished]",
                                                     Arg.Any<List<DbParameter>>()))
                                         .Do(callback => { actualParameters = callback.Arg<List<DbParameter>>(); });

            systemUnderTest.StatsUploadFinished(transaction, 100);

            Assert.That(actualParameters.Count, Is.EqualTo(1));
            Assert.That(actualParameters[0].ParameterName, Is.EqualTo("@DownloadId"));
            Assert.That(actualParameters[0].DbType, Is.EqualTo(DbType.Int32));
            Assert.That(actualParameters[0].Direction, Is.EqualTo(ParameterDirection.Input));
            Assert.That(actualParameters[0].Value, Is.EqualTo(100));
        }

        [Test]
        public void StatsUploadFinished_WhenInvoked_UpdatesStatsUploadToFinished()
        {
            var transaction = Substitute.For<DbTransaction>();

            systemUnderTest.StatsUploadFinished(transaction, 100);

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("StatsUploadFinished Invoked");
                loggingServiceMock.LogVerbose("Database connection was successful");
                databaseConnectionServiceMock.ExecuteStoredProcedure(transaction, "[FoldingCoin].[StatsUploadFinished]",
                    Arg.Any<List<DbParameter>>());
            });
        }

        [Test]
        public void UpdateToLatest_WhenInvoked_DatabaseUpdatedToLatest()
        {
            databaseConnectionServiceMock.ExecuteStoredProcedure(Arg.Any<string>())
                                         .Returns(NumberOfRowsEffectedExpected);

            InvokeUpdateToLatest();

            Received.InOrder(() =>
            {
                loggingServiceMock.LogVerbose("UpdateToLatest Invoked");
                loggingServiceMock.LogVerbose("Database connection was successful");
                databaseConnectionServiceMock.ExecuteStoredProcedure("[FoldingCoin].[UpdateToLatest]");
                loggingServiceMock.LogVerbose($"'{NumberOfRowsEffectedExpected}' rows were effected");
            });
        }

        private void InvokeFileDownloadError()
        {
            systemUnderTest.FileDownloadError(fileDownloadResult);
        }

        private void InvokeFileDownloadFinished()
        {
            systemUnderTest.FileDownloadFinished(filePayload);
        }

        private DateTime InvokeGetLastFileFownloadDateTime()
        {
            return systemUnderTest.GetLastFileDownloadDateTime();
        }

        private bool InvokeIsAvailable()
        {
            return systemUnderTest.IsAvailable();
        }

        private void InvokeNewFileDownloadStarted()
        {
            systemUnderTest.NewFileDownloadStarted(filePayload);
        }

        private void InvokeUpdateToLatest()
        {
            systemUnderTest.UpdateToLatest();
        }

        private IStatsDownloadDatabaseService NewFileDownloadDataStoreProvider(
            IDatabaseConnectionSettingsService databaseConnectionSettingsService,
            IDatabaseConnectionServiceFactory databaseConnectionServiceFactory, ILoggingService loggingService,
            IErrorMessageService errorMessageService)
        {
            return new StatsDownloadDatabaseProvider(databaseConnectionSettingsService,
                databaseConnectionServiceFactory,
                loggingService, errorMessageService);
        }

        private void SetUpDatabaseConnectionCreateDbCommandMock(
            Action<DbCommand>[] additionalCreateDbCommandSetupActions = null,
            Action<List<DbParameter>>[] additionalAddRangeSetUpActions = null)
        {
            var createCommandCallCount = 0;
            var addRangeCallCount = 0;

            databaseConnectionServiceMock.CreateDbCommand().Returns(createDbCommandInfo =>
            {
                var command = Substitute.For<DbCommand>();
                command.Parameters.Returns(parametersInfo =>
                {
                    var parameters = Substitute.For<DbParameterCollection>();
                    parameters.When(collection => collection.AddRange(Arg.Any<Array>())).Do(addRangeInfo =>
                    {
                        if (addRangeCallCount + 1 <= additionalAddRangeSetUpActions?.Length)
                        {
                            additionalAddRangeSetUpActions[addRangeCallCount]?.Invoke(
                                addRangeInfo.Arg<Array>().Cast<DbParameter>().ToList());
                            addRangeCallCount++;
                        }
                    });
                    return parameters;
                });

                if (createCommandCallCount + 1 <= additionalCreateDbCommandSetupActions?.Length)
                {
                    additionalCreateDbCommandSetupActions[createCommandCallCount]?.Invoke(command);
                    createCommandCallCount++;
                }

                return command;
            });
        }
    }
}