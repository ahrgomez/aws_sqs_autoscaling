using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

//Amazon
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon;

namespace AWS_SQS_AutoScale.Startup
{
    class Program
    {
        static void Main(string[] args)
        {
            Sqs_AutoScale.Configure();
        }
    }
}
