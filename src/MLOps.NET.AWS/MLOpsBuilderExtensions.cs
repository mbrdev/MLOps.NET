﻿using Amazon;
using Amazon.S3;
using MLOps.NET.Storage;

namespace MLOps.NET.AWS
{
    /// <summary>
    /// Extension methods to allow the usage of AWS storage
    /// </summary>
    public static class MLOpsBuilderExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="awsAccessKeyId"></param>
        /// <param name="awsSecretAccessKey"></param>
        /// <param name="regionName"></param>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        public static MLOpsBuilder UseAWSS3Repository(this MLOpsBuilder builder, string awsAccessKeyId, string awsSecretAccessKey, string regionName, string bucketName)
        {
            var region = RegionEndpoint.GetBySystemName(regionName);
            var  amazonS3Client = new AmazonS3Client(awsAccessKeyId, awsSecretAccessKey, region);
            builder.UseModelRepository(new S3BucketModelRepository(amazonS3Client, bucketName));

            return builder;
        }
    }
}