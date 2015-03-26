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
using System.Diagnostics;
using System.Linq;
using Microsoft.ServiceBus.Messaging; 
using System.Threading.Tasks;
#endregion

namespace Microsoft.AzureCat.ServiceBusExtensions
{
    /// <summary>
    /// This class contains extensions methods for the EventHubClient class
    /// </summary>
    public static class EventHubClientExtensions
    {
        #region Private Constants
        //*******************************
        // Formats
        //*******************************
        private const string EventDataListCannotBeNullOrEmpty = "The eventDataEnumerable parameter cannot be null or empty.";
        private const string SendPartitionedBatchFormat = "[EventHubClient.SendPartitionedBatch] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]";
        private const string SendPartitionedBatchAsyncFormat = "[EventHubClient.SendPartitionedBatchAsync] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]";
        #endregion

        #region Public Methods
        /// <summary>
        /// Asynchronously sends a batch of event data to the same partition.
        /// All the event data in the batch need to have the same value in the Partitionkey property.
        /// If the batch size is greater than the maximum batch size, 
        /// the method partitions the original batch into multiple batches, 
        /// each smaller in size than the maximum batch size.
        /// </summary>
        /// <param name="eventHubClient">The current EventHubClient object.</param>
        /// <param name="eventDataEnumerable">An IEnumerable object containing event data instances.</param>
        /// <param name="trace">true to cause a message to be written; otherwise, false.</param>
        /// <returns>The asynchronous operation.</returns>
        public async static Task SendPartitionedBatchAsync(this EventHubClient eventHubClient, IEnumerable<EventData> eventDataEnumerable, bool trace = false)
        {
            var eventDataList = eventDataEnumerable as IList<EventData> ?? eventDataEnumerable.ToList();
            if (eventDataEnumerable == null || !eventDataList.Any())
            {
                throw new ArgumentNullException(EventDataListCannotBeNullOrEmpty);
            }

            var batchList = new List<EventData>();
            long batchSize = 0;

            foreach (var eventData in eventDataList)
            {
                if ((batchSize + eventData.SerializedSizeInBytes) > Constants.MaxBathSizeInBytes)
                {
                    // Send current batch
                    await eventHubClient.SendBatchAsync(batchList);
                    Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));

                    // Initialize a new batch
                    batchList = new List<EventData> { eventData };
                    batchSize = eventData.SerializedSizeInBytes;
                }
                else
                {
                    // Add the EventData to the current batch
                    batchList.Add(eventData);
                    batchSize += eventData.SerializedSizeInBytes;
                }
            }
            // The final batch is sent outside of the loop
            await eventHubClient.SendBatchAsync(batchList);
            Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));
        }

        /// <summary>
        /// Asynchronously sends a batch of event data to the same partition.
        /// All the event data in the batch need to have the same value in the Partitionkey property.
        /// If the batch size is greater than the maximum batch size, 
        /// the method partitions the original batch into multiple batches, 
        /// each smaller in size than the maximum batch size.
        /// </summary>
        /// <param name="eventHubClient">The current EventHubClient object.</param>
        /// <param name="eventDataEnumerable">An IEnumerable object containing event data instances.</param>
        /// <param name="trace">true to cause a message to be written; otherwise, false.</param>
        public static void SendPartitionedBatch(this EventHubClient eventHubClient, IEnumerable<EventData> eventDataEnumerable, bool trace = false)
        {
            var eventDataList = eventDataEnumerable as IList<EventData> ?? eventDataEnumerable.ToList();
            if (eventDataEnumerable == null || !eventDataList.Any())
            {
                throw new ArgumentNullException(EventDataListCannotBeNullOrEmpty);
            }

            var batchList = new List<EventData>();
            long batchSize = 0;

            foreach (var eventData in eventDataList)
            {
                if ((batchSize + eventData.SerializedSizeInBytes) > Constants.MaxBathSizeInBytes)
                {
                    // Send current batch
                    eventHubClient.SendBatch(batchList);
                    Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));

                    // Initialize a new batch
                    batchList = new List<EventData> { eventData };
                    batchSize = eventData.SerializedSizeInBytes;
                }
                else
                {
                    // Add the EventData to the current batch
                    batchList.Add(eventData);
                    batchSize += eventData.SerializedSizeInBytes;
                }
            }
            // The final batch is sent outside of the loop
            eventHubClient.SendBatch(batchList);
            Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));
        } 
        #endregion
    }
}
