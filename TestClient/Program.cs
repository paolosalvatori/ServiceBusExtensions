#region Copyright
//=======================================================================================
// Microsoft Azure Customer Advisory Team  
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/b/paolos/. 
// 
// Author: Paolo Salvatori
//=======================================================================================
// Copyright © 2015 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
//=======================================================================================
#endregion

#region Using Directives
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

#endregion

namespace Microsoft.AzureCat.ServiceBusExtensions.TestClient
{
    /// <summary>
    /// This class can be used to test the extensions methods defines in the ServiceBusExtensions library.
    /// </summary>
    public class Program
    {
        #region Private Constants
        //***************************
        // Configuration Parameters
        //***************************
        private const string ConnectionString = "connectionString";
        private const string MessageSizeInBytes = "messageSizeInBytes";
        private const string MessageCountInBatch = "messageCountInBatch";

        //***************************
        // Entities
        //***************************
        private const string QueueName = "batchtestqueue";
        private const string TopicName = "batchtesttopic";
        private const string SubscriptionName = "auditing";
        private const string EventHubName = "batchtesteventhub";

        //***************************
        // Default Values
        //***************************
        private const int DefaultMessageCountInBatch = 100;
        private const int DefaultMessageSizeInBytes = 16384;

        //***************************
        // Formats
        //***************************
        private const string ParameterFormat = "{0}: [{1}]";
        private const string PressKeyToExit = "Press a key to exit.";
        private const string MenuChoiceFormat = "Select a numeric key between 1 and {0}";
        private const string ConnectionStringCannotBeNull = "The Service Bus connection string has not been defined in the configuration file.";
        private const string QueueCreatedFormat = "Queue [{0}] successfully created.";
        private const string TopicCreatedFormat = "Topic [{0}] successfully created.";
        private const string SubscriptionCreatedFormat = "Subscription [{0}] successfully created.";
        private const string EventHubCreatedFormat = "Event Hub [{0}] successfully created.";
        private const string QueueAlreadyExistsFormat = "Queue [{0}] already exists.";
        private const string TopicAlreadyExistsFormat = "Topic [{0}] already exists.";
        private const string SubscriptionAlreadyExistsFormat = "Subscription [{0}] already exists.";
        private const string EventHubAlreadyExistsFormat = "Event Hub [{0}] already exists.";
        private const string CallingMessageSenderSendBatchAsync = "Calling MessageSender.SendBatchAsync...";
        private const string MessageSenderSendBatchAsyncCalled = "MessageSender.SendBatchAsync called.";
        private const string CallingMessageSenderSendPartitionedBatchAsync = "Calling MessageSender.SendPartitionedBatchAsync...";
        private const string MessageSenderSendPartitionedBatchAsyncCalled = "MessageSender.SendPartitionedBatchAsync called.";
        private const string CallingQueueClientSendBatchAsync = "Calling QueueClient.SendBatchAsync...";
        private const string QueueClientSendBatchAsyncCalled = "QueueClient.SendBatchAsync called.";
        private const string CallingQueueClientSendPartitionedBatchAsync = "Calling QueueClient.SendPartitionedBatchAsync...";
        private const string QueueClientSendPartitionedBatchAsyncCalled = "QueueClient.SendPartitionedBatchAsync called.";
        private const string CallingTopicClientSendBatchAsync = "Calling TopicClient.SendBatchAsync...";
        private const string TopicClientSendBatchAsyncCalled = "TopicClient.SendBatchAsync called.";
        private const string CallingTopicClientSendPartitionedBatchAsync = "Calling TopicClient.SendPartitionedBatchAsync...";
        private const string TopicClientSendPartitionedBatchAsyncCalled = "TopicClient.SendPartitionedBatchAsync called.";
        private const string CallingEventHubClientSendBatchAsync = "Calling EventHubClient.SendBatchAsync...";
        private const string EventHubClientSendBatchAsyncCalled = "EventHubClient.SendBatchAsync called.";
        private const string CallingEventHubClientSendPartitionedBatchAsync = "Calling EventHubClient.SendPartitionedBatchAsync...";
        private const string EventHubClientSendPartitionedBatchAsyncCalled = "EventHubClient.SendPartitionedBatchAsync called.";

        //***************************
        // Menu Items
        //***************************
        private const string MessageSenderSendPartitionedBatchAsyncTest = "MessageSender.SendPartitionedBatchAsync Test";
        private const string QueueClientSendPartitionedBatchAsyncTest = "QueueClient.SendPartitionedBatchAsync Test";
        private const string TopicClientSendPartitionedBatchAsyncTest = "TopicClient.SendPartitionedBatchAsync Test";
        private const string EventHubClientSendPartitionedBatchAsyncTest = "EventHubClient.SendPartitionedBatchAsync Test";
        private const string Exit = "Exit";
        #endregion

        #region Private Static Fields
        private static string connectionString;
        private static int messageSizeInBytes;
        private static int messageCountInBatch;
        private static MessagingFactory messagingFactory;
        private static readonly List<string> menuItemList = new List<string>
        {
            MessageSenderSendPartitionedBatchAsyncTest ,
            QueueClientSendPartitionedBatchAsyncTest,
            TopicClientSendPartitionedBatchAsyncTest,
            EventHubClientSendPartitionedBatchAsyncTest,
            Exit
        }; 
        #endregion

        #region Main Method
        public static void Main()
        {
            try
            {
                if (ReadConfiguration() && CreateEntitiesAsync().Result)
                {
                    // Add ConsoleTraceListener
                    Trace.Listeners.Add(new ConsoleTraceListener());

                    // Create MessagingFactory object
                    messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);

                    int key;
                    while ((key = ShowMenu()) != menuItemList.Count - 1)
                    {
                        switch (menuItemList[key])
                        {
                            case MessageSenderSendPartitionedBatchAsyncTest:
                                // Test MessageSender.SendPartitionedBatchAsync method
                                MessageSenderTest().Wait();
                                break;
                            case QueueClientSendPartitionedBatchAsyncTest:
                                // Test QueueClient.SendPartitionedBatchAsync method
                                QueueClientTest().Wait();
                                break;
                            case TopicClientSendPartitionedBatchAsyncTest:
                                // Test TopicClient.SendPartitionedBatchAsync method
                                TopicClientTest().Wait();
                                break;
                            case EventHubClientSendPartitionedBatchAsyncTest:
                                // Test EventHubClient.SendPartitionedBatchAsync method
                                EventHubClientTest().Wait();
                                break;
                            case Exit:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            PrintMessage(PressKeyToExit);
            Console.ReadLine();
        } 
        #endregion

        #region Private Static Methods
        private async static Task MessageSenderTest()
        {
            // Create MessageSender object
            var messageSender = await messagingFactory.CreateMessageSenderAsync(QueueName);

            // Test MessageSender.SendBatchAsync: if the batch size is greater than the max batch size
            // the method throws a  MessageSizeExceededException
            try
            {
                PrintMessage(CallingMessageSenderSendBatchAsync);
                await messageSender.SendBatchAsync(CreateBrokeredMessageBatch());
                PrintMessage(MessageSenderSendBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }

            try
            {
                // Send the batch using the SendPartitionedBatchAsync method
                PrintMessage(CallingMessageSenderSendPartitionedBatchAsync);
                await messageSender.SendPartitionedBatchAsync(CreateBrokeredMessageBatch(), true);
                PrintMessage(MessageSenderSendPartitionedBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private async static Task QueueClientTest()
        {
            // Create QueueClient object
            var queueClient = messagingFactory.CreateQueueClient(QueueName);

            // Test QueueClient.SendBatchAsync: if the batch size is greater than the max batch size
            // the method throws a  MessageSizeExceededException
            try
            {
                PrintMessage(CallingQueueClientSendBatchAsync);
                await queueClient.SendBatchAsync(CreateBrokeredMessageBatch());
                PrintMessage(QueueClientSendBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }

            try
            {
                // Send the batch using the SendPartitionedBatchAsync method
                PrintMessage(CallingQueueClientSendPartitionedBatchAsync);
                await queueClient.SendPartitionedBatchAsync(CreateBrokeredMessageBatch(), true);
                PrintMessage(QueueClientSendPartitionedBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private async static Task TopicClientTest()
        {
            // Create TopicClient object
            var topicClient = messagingFactory.CreateTopicClient(TopicName);

            // Test TopicClient.SendBatchAsync: if the batch size is greater than the max batch size
            // the method throws a  MessageSizeExceededException
            try
            {
                PrintMessage(CallingTopicClientSendBatchAsync);
                await topicClient.SendBatchAsync(CreateBrokeredMessageBatch());
                PrintMessage(TopicClientSendBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }

            try
            {
                // Send the batch using the SendPartitionedBatchAsync method
                PrintMessage(CallingTopicClientSendPartitionedBatchAsync);
                await topicClient.SendPartitionedBatchAsync(CreateBrokeredMessageBatch(), true);
                PrintMessage(TopicClientSendPartitionedBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private async static Task EventHubClientTest()
        {
            // Create EventHubClient object
            var eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, EventHubName);

            // Test EventHubClient.SendBatchAsync: if the batch size is greater than the max batch size
            // the method throws a  MessageSizeExceededException
            try
            {
                PrintMessage(CallingEventHubClientSendBatchAsync);
                await eventHubClient.SendBatchAsync(CreateEventDataBatch());
                PrintMessage(EventHubClientSendBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }

            try
            {
                // Send the batch using the SendPartitionedBatchAsync method
                PrintMessage(CallingEventHubClientSendPartitionedBatchAsync);
                await eventHubClient.SendPartitionedBatchAsync(CreateEventDataBatch(), true);
                PrintMessage(EventHubClientSendPartitionedBatchAsyncCalled);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static IEnumerable<BrokeredMessage> CreateBrokeredMessageBatch()
        {
            var messageList = new List<BrokeredMessage>();
            for (var i = 0; i < messageCountInBatch; i++)
            {
                messageList.Add(new BrokeredMessage(Encoding.UTF8.GetBytes(new string('A', messageSizeInBytes))));
            }
            return messageList;
        }

        private static IEnumerable<EventData> CreateEventDataBatch()
        {
            var messageList = new List<EventData>();
            for (var i = 0; i < messageCountInBatch; i++)
            {
                // Note: the partition key in this sample is null.
                // it's mandatory that all event data in a batch have the same PartitionKey
                messageList.Add(new EventData(Encoding.UTF8.GetBytes(new string('A', messageSizeInBytes))));
            }
            return messageList;
        }

        private async static Task<bool> CreateEntitiesAsync()
        {
            try
            {
                // Create NamespaceManeger object
                var namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
                
                // Create test queue
                if (!await namespaceManager.QueueExistsAsync(QueueName))
                {
                    await namespaceManager.CreateQueueAsync(new QueueDescription(QueueName)
                    {
                        EnableBatchedOperations = true,
                        EnableExpress = true,
                        EnablePartitioning = true,
                        EnableDeadLetteringOnMessageExpiration = true
                    });
                    PrintMessage(string.Format(QueueCreatedFormat, QueueName));
                }
                else
                {
                    PrintMessage(string.Format(QueueAlreadyExistsFormat, QueueName));
                }
                
                // Create test topic
                if (!await namespaceManager.TopicExistsAsync(TopicName))
                {
                    await namespaceManager.CreateTopicAsync(new TopicDescription(TopicName)
                    {
                        EnableBatchedOperations = true,
                        EnableExpress = true,
                        EnablePartitioning = true,
                    });
                    PrintMessage(string.Format(TopicCreatedFormat, TopicName));
                }
                else
                {
                    PrintMessage(string.Format(TopicAlreadyExistsFormat, TopicName));
                }
                
                // Create test subscription
                if (!await namespaceManager.SubscriptionExistsAsync(TopicName, SubscriptionName))
                {
                    await namespaceManager.CreateSubscriptionAsync(new SubscriptionDescription(TopicName, SubscriptionName)
                    {
                        EnableBatchedOperations = true
                    }, new RuleDescription(new TrueFilter()));
                    PrintMessage(string.Format(SubscriptionCreatedFormat, SubscriptionName));
                }
                else
                {
                    PrintMessage(string.Format(SubscriptionAlreadyExistsFormat, SubscriptionName));
                }

                // Create test event hub
                if (!await namespaceManager.EventHubExistsAsync(EventHubName))
                {
                    await namespaceManager.CreateEventHubAsync(new EventHubDescription(EventHubName)
                    {
                        PartitionCount = 16,
                        MessageRetentionInDays = 1
                    });
                    PrintMessage(string.Format(EventHubCreatedFormat, EventHubName));
                }
                else
                {
                    PrintMessage(string.Format(EventHubAlreadyExistsFormat, EventHubName));
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
                return false;
            }
            return true;
        }

        private static int ShowMenu()
        {
            // Print Menu Header
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Menu");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("]");
            Console.ResetColor();

            // Print Menu Items
            for (var i = 0; i < menuItemList.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("{0}", i + 1);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("]");
                Console.ResetColor();
                Console.Write(": ");
                Console.WriteLine(menuItemList[i]);
                Console.ResetColor();
            }

            // Select an option
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(MenuChoiceFormat, menuItemList.Count);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]");
            Console.ResetColor();
            Console.Write(": ");

            var key = 'a';
            while (key < '1' || key > '9')
            {
                key = Console.ReadKey(true).KeyChar;
            }
            Console.WriteLine();
            return key - '1';
        }

        private static bool ReadConfiguration()
        {
            try
            {
                // Set window size
                Console.SetWindowSize(120, 40);

                // Read connectionString setting
                connectionString = ConfigurationManager.AppSettings[ConnectionString];
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new ArgumentException(ConnectionStringCannotBeNull);
                }
                PrintMessage(string.Format(ParameterFormat, ConnectionString, connectionString));

                // Read messageSizeInBytes setting
                int value;
                var setting = ConfigurationManager.AppSettings[MessageSizeInBytes];
                messageSizeInBytes = int.TryParse(setting, out value) ?
                                value :
                                DefaultMessageSizeInBytes;
                PrintMessage(string.Format(ParameterFormat, MessageSizeInBytes, messageSizeInBytes));

                // Read messageCountInBatch setting
                setting = ConfigurationManager.AppSettings[MessageCountInBatch];
                messageCountInBatch = int.TryParse(setting, out value) ?
                                value :
                                DefaultMessageCountInBatch;
                PrintMessage(string.Format(ParameterFormat, MessageCountInBatch, messageCountInBatch));
                return true;
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
            return false;
        }

        private static void PrintMessage(string message, [CallerMemberName] string memberName = "")
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(memberName))
            {
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(memberName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]");
            Console.ResetColor();
            Console.Write(": ");
            Console.WriteLine(message);
        }

        private static void PrintException(Exception ex,
                                           [CallerFilePath] string sourceFilePath = "",
                                           [CallerMemberName] string memberName = "",
                                           [CallerLineNumber] int sourceLineNumber = 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string fileName = null;
            if (File.Exists(sourceFilePath))
            {
                var file = new FileInfo(sourceFilePath);
                fileName = file.Name;
            }
            Console.Write(string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(string.IsNullOrWhiteSpace(memberName) ? "Unknown" : memberName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(sourceLineNumber.ToString(CultureInfo.InvariantCulture));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]");
            Console.ResetColor();
            Console.Write(": ");
            Console.WriteLine(ex != null && !string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "An error occurred.");
            var aggregateException = ex as AggregateException;
            if (aggregateException == null)
            {
                return;
            }
            foreach (var exception in aggregateException.InnerExceptions)
            {
                PrintException(exception);
            }
        }
        #endregion
    }
}
