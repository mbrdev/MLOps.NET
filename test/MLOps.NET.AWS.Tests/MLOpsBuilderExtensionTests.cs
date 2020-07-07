﻿using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MLOps.NET.Catalogs;
using MLOps.NET.Storage;
using Moq;
using System.Reflection;

namespace MLOps.NET.AWS.Tests
{
    [TestCategory("UnitTests")]
    [TestClass]
    public class MLOpsBuilderExtensionTests
    {
        [TestMethod]
        public void UseAzureStorage_ConfiguresModelCatalog()
        {
            //Act
            IMLOpsContext unitUnderTest = new MLOpsBuilder()
                .UseAWSS3Repository("access-key-id","secret-access-key","region-name","bucket-name")
                .UseMetaDataStore(new Mock<IMetaDataStore>().Object)
                .Build();

            unitUnderTest.Should().BeOfType<MLOpsContext>("Because the default IMLOpsContext is MLOpsContext");

            //Assert
            unitUnderTest.Model.Should().NotBeNull();

            var repositoryField = typeof(ModelCatalog).GetField("modelRepository", BindingFlags.Instance | BindingFlags.NonPublic);
            repositoryField.GetValue(unitUnderTest.Model).Should().BeOfType<S3BucketModelRepository>();
        }
    }
}
