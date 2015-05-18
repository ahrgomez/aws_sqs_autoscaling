using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//Amazon AWS SDK
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon;


namespace AWS_SQS_AutoScale.Startup
{
    public static class Sqs_AutoScale
    {
        public static void Configure()
        {
            //Amazon Credentials
            string accessKey = ConfigurationManager.AppSettings["AWSAccessKeyId"];
            string secretKey = ConfigurationManager.AppSettings["AWSSecretKey"];

            string launchConfigurationName = "ScheduledServicesLaunchConfiguration";

            string autoScalingGroupName = "ScheduledServiceASG";
            string scaleOutPolicyName = "ScheduledServicesScaleOutSQSPolicy";
            string scaleInPolicyName = "ScheduledServicesScaleInSQSPolicy";

            string scaleOutARN = "";
            string scaleInARN = "";

            string queueName = "ScheduledServicesSQS";
            string queueURL = "";

            string amiID = "AMI_ID";
            string instanceType = "INSTANCE_TYPE";

            AmazonAutoScalingClient autoScaleClient = new AmazonAutoScalingClient(accessKey, secretKey, RegionEndpoint.USWest2);

            AmazonSQSClient sqsClient = new AmazonSQSClient(accessKey, secretKey, RegionEndpoint.USWest2);

            Console.WriteLine("¡¡¡CONFIGURATION INITIALIZED!!!");
            Console.WriteLine("");

            Console.WriteLine("--------- SQS ---------");
            Console.WriteLine("");

            Console.WriteLine("Creating the simple queue service item");
            Console.WriteLine("");

            //Get or create the sqs instance
            CreateQueueRequest createQueueRequest = new CreateQueueRequest(queueName);
            CreateQueueResponse createQueueResponse = sqsClient.CreateQueue(createQueueRequest);
            queueURL = createQueueResponse.QueueUrl;

            Console.WriteLine("Created the simple queue service item with name: " + queueName + " and  url: " + queueURL);
            Console.WriteLine("");

            Console.WriteLine("--------- EC2 ---------");
            Console.WriteLine("");

            //If not exists any launch configuration with this name, creates
            DescribeLaunchConfigurationsRequest describeLaunchConfigurationsRequest = new DescribeLaunchConfigurationsRequest();
            describeLaunchConfigurationsRequest.LaunchConfigurationNames = new List<string>() { launchConfigurationName };
            DescribeLaunchConfigurationsResponse describeLaunchConfigurationsResponse = autoScaleClient.DescribeLaunchConfigurations(describeLaunchConfigurationsRequest);

            Console.WriteLine("Creating the launch configuration");
            Console.WriteLine("");
            if (describeLaunchConfigurationsResponse.LaunchConfigurations.Count == 0)
            {
                //Create Launch Configuration Request
                CreateLaunchConfigurationRequest launchConfigurationRequest = new CreateLaunchConfigurationRequest();
                launchConfigurationRequest.LaunchConfigurationName = launchConfigurationName;
                launchConfigurationRequest.ImageId = amiID;
                launchConfigurationRequest.InstanceType = instanceType;

                //Create Launch Configuration Response
                CreateLaunchConfigurationResponse launchConfigurationResponse = autoScaleClient.CreateLaunchConfiguration(launchConfigurationRequest);

                Console.WriteLine("Created the launch configuration with name: " + launchConfigurationName);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Founded the launch configuration with name: " + launchConfigurationName);
                Console.WriteLine("");
            }

            Console.WriteLine("Creating the autoscaling group");
            Console.WriteLine("");


            DescribeAutoScalingGroupsRequest describeAutoScalingGroupsRequest = new DescribeAutoScalingGroupsRequest();
            describeAutoScalingGroupsRequest.AutoScalingGroupNames = new List<string>() { autoScalingGroupName };
            DescribeAutoScalingGroupsResponse describeAutoScalingGroupsResponse = autoScaleClient.DescribeAutoScalingGroups(describeAutoScalingGroupsRequest);

            if (describeAutoScalingGroupsResponse.AutoScalingGroups.Count == 0)
            {
                //Create Auto Scaling Group Request
                CreateAutoScalingGroupRequest autoScalingGroupRequest = new CreateAutoScalingGroupRequest();
                autoScalingGroupRequest.AutoScalingGroupName = autoScalingGroupName;
                autoScalingGroupRequest.MinSize = 1;
                autoScalingGroupRequest.MaxSize = 3;
                autoScalingGroupRequest.DesiredCapacity = 1;
                autoScalingGroupRequest.AvailabilityZones = new List<string>() { "us-west-2a", "us-west-2b", "us-west-2c" };
                autoScalingGroupRequest.LaunchConfigurationName = launchConfigurationName;

                //Create Auto Scaling Group Response
                autoScaleClient.CreateAutoScalingGroup(autoScalingGroupRequest);

                Console.WriteLine("Created the autoscaling group with name: " + autoScalingGroupName);
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("Founded the autoscaling group with name: " + autoScalingGroupName);
                Console.WriteLine("");
            }

            Console.WriteLine("Creating the scale out policy");
            Console.WriteLine("");

            //Policies
            //Creating scaling out policy for the SQS
            PutScalingPolicyRequest scalingOutPolicyRequest = new PutScalingPolicyRequest();
            scalingOutPolicyRequest.PolicyName = scaleOutPolicyName;
            scalingOutPolicyRequest.AutoScalingGroupName = autoScalingGroupName;
            scalingOutPolicyRequest.ScalingAdjustment = -1;
            scalingOutPolicyRequest.AdjustmentType = "ChangeInCapacity";

            PutScalingPolicyResponse scalingOutPolicyResponse = autoScaleClient.PutScalingPolicy(scalingOutPolicyRequest);
            scaleOutARN = scalingOutPolicyResponse.PolicyARN;

            Console.WriteLine("Created the scale out policy with arn: " + scaleOutARN);
            Console.WriteLine("");

            Console.WriteLine("Creating the scale in policy");
            Console.WriteLine("");

            //Creating scaling in policy for the SQS
            PutScalingPolicyRequest scalingInPolicyRequest = new PutScalingPolicyRequest();
            scalingInPolicyRequest.PolicyName = scaleInPolicyName;
            scalingInPolicyRequest.AutoScalingGroupName = autoScalingGroupName;
            scalingInPolicyRequest.ScalingAdjustment = 1;
            scalingInPolicyRequest.AdjustmentType = "ChangeInCapacity";

            PutScalingPolicyResponse scalingInPolicyResponse = autoScaleClient.PutScalingPolicy(scalingInPolicyRequest);
            scaleInARN = scalingInPolicyResponse.PolicyARN;

            Console.WriteLine("Created the scale in policy with arn: " + scaleInARN);
            Console.WriteLine("");

            AmazonCloudWatchClient cloudWatchClient = new AmazonCloudWatchClient(accessKey, secretKey, RegionEndpoint.USWest2);

            Console.WriteLine("--------- CLOUD WATCH ---------");
            Console.WriteLine("");

            Console.WriteLine("Creating the scale in policy");
            Console.WriteLine("");

            //Scale In
            PutMetricAlarmRequest metricAlarmScaleInRequest = new PutMetricAlarmRequest();
            metricAlarmScaleInRequest.AlarmName = "ScheduledServicesScaleInMetric";
            metricAlarmScaleInRequest.MetricName = "ApproximateNumberOfMessagesVisible";
            metricAlarmScaleInRequest.Namespace = "AWS/SQS";
            metricAlarmScaleInRequest.Period = 300;
            metricAlarmScaleInRequest.Threshold = 3;
            metricAlarmScaleInRequest.ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold;
            metricAlarmScaleInRequest.Statistic = new Statistic("Average");

            Dimension dimensionScaleIn = new Dimension();
            dimensionScaleIn.Name = "QueueName";
            dimensionScaleIn.Value = queueName;

            metricAlarmScaleInRequest.Dimensions.Add(dimensionScaleIn);
            metricAlarmScaleInRequest.EvaluationPeriods = 2;
            metricAlarmScaleInRequest.AlarmActions.Add(scaleInARN);

            cloudWatchClient.PutMetricAlarm(metricAlarmScaleInRequest);

            Console.WriteLine("Created the scale in policy with name: ScheduledServicesScaleInMetric");
            Console.WriteLine("");

            Console.WriteLine("Creating the scale out policy");
            Console.WriteLine("");

            //Scale Out
            PutMetricAlarmRequest metricAlarmScaleOutRequest = new PutMetricAlarmRequest();
            metricAlarmScaleOutRequest.AlarmName = "ScheduledServicesScaleOutMetric";
            metricAlarmScaleOutRequest.MetricName = "ApproximateNumberOfMessagesVisible";
            metricAlarmScaleOutRequest.Namespace = "AWS/SQS";
            metricAlarmScaleOutRequest.Period = 300;
            metricAlarmScaleOutRequest.Threshold = 3;
            metricAlarmScaleOutRequest.ComparisonOperator = ComparisonOperator.LessThanThreshold;
            metricAlarmScaleOutRequest.Statistic = new Statistic("Average");

            Dimension dimensionScaleOut = new Dimension();
            dimensionScaleOut.Name = "QueueName";
            dimensionScaleOut.Value = queueName;

            metricAlarmScaleOutRequest.Dimensions.Add(dimensionScaleOut);
            metricAlarmScaleOutRequest.EvaluationPeriods = 2;
            metricAlarmScaleOutRequest.AlarmActions.Add(scaleOutARN);

            cloudWatchClient.PutMetricAlarm(metricAlarmScaleOutRequest);

            Console.WriteLine("Created the scale out policy with name: ScheduledServicesScaleOutMetric");
            Console.WriteLine("");

            Console.WriteLine("¡¡¡CONFIGURATION FINISHED!!!");
        }
    }
}
