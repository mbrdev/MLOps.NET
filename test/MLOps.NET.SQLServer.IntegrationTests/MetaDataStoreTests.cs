﻿using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MLOps.NET.Entities;
using MLOps.NET.Storage;
using MLOps.NET.Tests.Common.Data;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MLOps.NET.SQLServer.IntegrationTests
{
    [TestClass]
    [TestCategory("IntegrationTestSqlServer")]
    public class MetaDataStoreTests
    {
        private const string connectionString = "Server=localhost,1433;Database=MLOpsNET_IntegrationTests;User Id=sa;Password=MLOps4TheWin!;";
        private IMLOpsContext sut;

        [TestInitialize]
        public void Initialize()
        {
            sut = new MLOpsBuilder()
                .UseModelRepository(new Mock<IModelRepository>().Object)
                .UseSQLServer(connectionString)
                .Build();
        }

        [TestCleanup]
        public async Task TearDown()
        {
            var options = new DbContextOptionsBuilder()
                .UseSqlServer(connectionString)
                .Options;

            var contextFactory = new DbContextFactory(options);
            var context = contextFactory.CreateDbContext();

            var experiments = context.Experiments;
            var runs = context.Runs;
            var metrics = context.Metrics;
            var hyperParameters = context.HyperParameters;
            var confusionMatrices = context.ConfusionMatrices;
            var data = context.Data;
            var dataSchema = context.DataSchemas;
            var dataColumns = context.DataColumns;

            context.Experiments.RemoveRange(experiments);
            context.Runs.RemoveRange(runs);
            context.Metrics.RemoveRange(metrics);
            context.HyperParameters.RemoveRange(hyperParameters);
            context.ConfusionMatrices.RemoveRange(confusionMatrices);
            context.Data.RemoveRange(data);
            context.DataSchemas.RemoveRange(dataSchema);
            context.DataColumns.RemoveRange(dataColumns);

            await context.SaveChangesAsync();
        }

        [TestMethod]
        public async Task CreateExperimentAsync_ShouldCreateAnExperiment()
        {
            //Act
            var id = await sut.LifeCycle.CreateExperimentAsync("test");

            //Assert
            var experiement = sut.LifeCycle.GetExperiment("test");
            experiement.Should().NotBeNull();
            experiement.Id.Should().Be(id);
        }

        [TestMethod]
        public async Task CreateExperimentAsync_Twice_ShouldNotAddDuplicate()
        {
            //Act
            var experimentId = await sut.LifeCycle.CreateExperimentAsync("test");
            var experimentId2 = await sut.LifeCycle.CreateExperimentAsync("test");

            //Assert
            experimentId.Should().Be(experimentId2);
        }

        [TestMethod]
        public async Task CreateRunAsync_ShouldCreateRun()
        {
            //Act
            var experimentId = await sut.LifeCycle.CreateExperimentAsync("test");
            var id = await sut.LifeCycle.CreateRunAsync(experimentId);

            //Assert
            var run = sut.LifeCycle.GetRun(id);
            run.Should().NotBeNull();
            run.Id.Should().Be(id);
        }

        [TestMethod]
        public async Task LogMetricAsync_ShouldLogMetric()
        {
            //Arrange
            var experimentId = await sut.LifeCycle.CreateExperimentAsync("test");
            var id = await sut.LifeCycle.CreateRunAsync(experimentId);

            //Act
            await sut.Evaluation.LogMetricAsync(id, "F1Score", 0.78d);

            //Assert
            var metric = sut.Evaluation.GetMetrics(id).First();
            metric.Should().NotBeNull();
            metric.MetricName.Should().Be("F1Score");
            metric.Value.Should().Be(0.78d);
        }

        [TestMethod]
        public async Task SetTrainingTimeAsync_ShouldTrainingTime()
        {
            //Arrange
            var experimentId = await sut.LifeCycle.CreateExperimentAsync("test");
            var id = await sut.LifeCycle.CreateRunAsync(experimentId);

            var trainingTime = new System.TimeSpan(0, 5, 0);

            //Act
            await sut.LifeCycle.SetTrainingTimeAsync(id, trainingTime);

            //Assert
            var run = sut.LifeCycle.GetRun(id);
            run.TrainingTime.Should().Be(trainingTime);
        }

        //[TestMethod]
        //public async Task LogConfusionMatrixAsync_SavesConfusionMatrixOnRun()
        //{
        //    //Arrange
        //    var experimentId = await sut.LifeCycle.CreateExperimentAsync("test");
        //    var runId = await sut.LifeCycle.CreateRunAsync(experimentId);

        //    var expectedConfusionMatrix = new ConfusionMatrix
        //    {
        //        PerClassPrecision = new List<double> { 0.99d, 0.44d },
        //        PerClassRecall = new List<double> { 0.77d, 0.88d },
        //        Counts = new List<List<double>>
        //        {
        //            new List<double> { 9, 1 },
        //            new List<double> { 4, 33}
        //        },
        //        NumberOfClasses = 2
        //    };

        //    //Act
        //    await sut.Evaluation.LogConfusionMatrixAsync(runId, expectedConfusionMatrix);

        //    //Assert
        //    var confusionMatrix = sut.Evaluation.GetConfusionMatrix(runId);

        //    confusionMatrix.Should().NotBeNull();
        //    confusionMatrix.Should().BeEquivalentTo(expectedConfusionMatrix);
        //}

        [TestMethod]
        public async Task GetConfusionMatrix_NoConfusionMatrixExist_ShouldReturnNull()
        {
            //Arrange
            var experimentId = await sut.LifeCycle.CreateExperimentAsync("test");
            var runId = await sut.LifeCycle.CreateRunAsync(experimentId);

            //Act
            var confusionMatrix = sut.Evaluation.GetConfusionMatrix(runId);

            //Assert
            confusionMatrix.Should().BeNull();
        }

        [TestMethod]
        public async Task LogDataAsync_GivenValidDataView_ShouldLogData()
        {
            //Arrange
            var experimentId = await sut.LifeCycle.CreateExperimentAsync("test");
            var runId = await sut.LifeCycle.CreateRunAsync(experimentId);

            var data = LoadData();

            //Act
            await sut.Data.LogDataAsync(runId, data);

            //Assert
            var savedData = sut.Data.GetData(runId);

            savedData.DataSchema.ColumnCount.Should().Be(2);

            savedData.DataSchema.DataColumns
                .Any(x => x.Type == nameof(Boolean) && x.Name == "Sentiment")
                .Should()
                .BeTrue();

            savedData.DataSchema.DataColumns
                .Any(x => x.Type == nameof(String) && x.Name == "Review")
                .Should()
                .BeTrue();
        }

        private IDataView LoadData()
        {
            var mlContext = new MLContext(seed: 1);

            return mlContext.Data.LoadFromTextFile<ProductReview>("Data/product_reviews.csv", hasHeader: true, separatorChar: ',');
        }
    }
}
