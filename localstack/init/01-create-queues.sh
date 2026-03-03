#!/bin/bash
# LocalStack init script — runs automatically when LocalStack is ready
echo "Creating SQS queues..."
awslocal sqs create-queue --queue-name welcome-emails --region us-east-1
echo "Queue 'welcome-emails' created successfully"
awslocal sqs list-queues
