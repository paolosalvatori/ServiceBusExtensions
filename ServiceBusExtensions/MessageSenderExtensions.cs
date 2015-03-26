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
    /// This class contains extensions methods for the MessageSender class
    /// </summary>
    public static class MessageSenderClientExtensions
    {
        #region Private Constants
        //*******************************
        // Formats
        //*******************************
        private const string BrokeredMessageListCannotBeNullOrEmpty = "The brokeredMessageEnumerable parameter cannot be null or empty.";
        private const string SendPartitionedBatchFormat = "[MessageSender.SendPartitionedBatch] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]";
        private const string SendPartitionedBatchAsyncFormat = "[MessageSender.SendPartitionedBatchAsync] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]";
        #endregion

        #region Public Methods
        /// <summary>
        /// Sends a set of brokered messages (for batch processing). 
        /// If the batch size is greater than the maximum batch size, 
        /// the method partitions the original batch into multiple batches, 
        /// each smaller in size than the maximum batch size.
        /// </summary>
        /// <param name="messageSender">The current MessageSender object.</param>
        /// <param name="brokeredMessageEnumerable">The collection of brokered messages to send.</param>
        /// <param name="trace">true to cause a message to be written; otherwise, false.</param>
        /// <returns>The asynchronous operation.</returns>
        public async static Task SendPartitionedBatchAsync(this MessageSender messageSender, IEnumerable<BrokeredMessage> brokeredMessageEnumerable, bool trace = false)
        {
            var brokeredMessageList = brokeredMessageEnumerable as IList<BrokeredMessage> ?? brokeredMessageEnumerable.ToList();
            if (brokeredMessageEnumerable == null || !brokeredMessageList.Any())
            {
                throw new ArgumentNullException(BrokeredMessageListCannotBeNullOrEmpty);
            }

            var batchList = new List<BrokeredMessage>();
            long batchSize = 0;

            foreach (var brokeredMessage in brokeredMessageList)
            {
                if ((batchSize + brokeredMessage.Size) > Constants.MaxBathSizeInBytes)
                {
                    // Send current batch
                    await messageSender.SendBatchAsync(batchList);
                    Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));

                    // Initialize a new batch
                    batchList = new List<BrokeredMessage> { brokeredMessage };
                    batchSize = brokeredMessage.Size;
                }
                else
                {
                    // Add the BrokeredMessage to the current batch
                    batchList.Add(brokeredMessage);
                    batchSize += brokeredMessage.Size;
                }
            }
            // The final batch is sent outside of the loop
            await messageSender.SendBatchAsync(batchList);
            Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));
        }

        /// <summary>
        /// Sends a set of brokered messages (for batch processing). 
        /// If the batch size is greater than the maximum batch size, 
        /// the method partitions the original batch into multiple batches, 
        /// each smaller in size than the maximum batch size.
        /// </summary>
        /// <param name="messageSender">The current MessageSender object.</param>
        /// <param name="brokeredMessageEnumerable">The collection of brokered messages to send.</param>
        /// <param name="trace">true to cause a message to be written; otherwise, false.</param>
        public static void SendPartitionedBatch(this MessageSender messageSender, IEnumerable<BrokeredMessage> brokeredMessageEnumerable, bool trace = false)
        {
            var brokeredMessageList = brokeredMessageEnumerable as IList<BrokeredMessage> ?? brokeredMessageEnumerable.ToList();
            if (brokeredMessageEnumerable == null || !brokeredMessageList.Any())
            {
                throw new ArgumentNullException(BrokeredMessageListCannotBeNullOrEmpty);
            }

            var batchList = new List<BrokeredMessage>();
            long batchSize = 0;

            foreach (var brokeredMessage in brokeredMessageList)
            {
                if ((batchSize + brokeredMessage.Size) > Constants.MaxBathSizeInBytes)
                {
                    // Send current batch
                    messageSender.SendBatch(batchList);
                    Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));

                    // Initialize a new batch
                    batchList = new List<BrokeredMessage> { brokeredMessage };
                    batchSize = brokeredMessage.Size;
                }
                else
                {
                    // Add the BrokeredMessage to the current batch
                    batchList.Add(brokeredMessage);
                    batchSize += brokeredMessage.Size;
                }
            }
            // The final batch is sent outside of the loop
            messageSender.SendBatch(batchList);
            Trace.WriteLineIf(trace, string.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));
        }
        #endregion
    }
}