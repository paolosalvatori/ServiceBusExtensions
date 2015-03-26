<h1>Introduction</h1>

<p style="text-align: justify;">When a developer tries to use the SendBatch or SendBatchAsync methods exposed by the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.messagesender.aspx?f=255&amp;MSPPError=-2147217396">MessageSender</a>, <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.queueclient.aspx">QueueClient</a>, <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.topicclient.aspx">TopicClient</a>, and <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventhubclient.aspx">EventHubClient</a> classes contained in the <a href="https://msdn.microsoft.com/en-us/library/jj933424.aspx"> Microsoft.ServiceBus.dll</a>, and the batch size is greater than the maximum allowed size for a <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.brokeredmessage.aspx">BrokeredMessage</a> or an <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventdata.aspx">EventHub</a> object (at the time of writing, the limit is 256 KB), the method call throws a <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.messagesizeexceededexception.aspx">MessageSizeExceededException</a>. This library contains synchronous and asynchronous extension methods for the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.messagesender.aspx?f=255&amp;MSPPError=-2147217396">MessageSender</a>, <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.queueclient.aspx">QueueClient</a>,<a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.topicclient.aspx">TopicClient</a>, and <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventhubclient.aspx">EventHubClient</a> classes that allow to send a batch which size is greater than the maximum allowed size for a batch. In particular, the implementation partitions the batch into one or multiple batches, each smaller than the maximum size allowed, and sends them in a loop, to respect the chronological order of the messages contained in the original batch. The code can be found <a href="http://bit.ly/1EHg2Qp">here</a>.</p>

<h1>Solution</h1>

<p style="text-align: left;">The <strong>ServiceBusExtensions </strong>library contains 4 classes:</p>

<ul>
  <li><strong>MessageSenderExtensions</strong>: this class exposes the SendPartitionedBatch and SendPartitionedBatchAsync extension methods for the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.messagesender.aspx?f=255&amp;MSPPError=-2147217396">MessageSender</a> class. </li>

  <li><strong>QueueClientExtensions</strong>: this class exposes the SendPartitionedBatch andSendPartitionedBatchAsync extension methods for the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.queueclient.aspx">QueueClient</a> class. </li>

  <li><strong>TopicClientExtensions</strong>: this class exposes the SendPartitionedBatch andSendPartitionedBatchAsync extension methods for the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.topicclient.aspx">TopicClient</a> class. </li>

  <li><strong>EventHubClientExtensions</strong>: this class exposes the SendPartitionedBatch andSendPartitionedBatchAsync extension methods for the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventhubclient.aspx">EventHubClient</a> class. </li>
</ul>

<p>The <strong>TesterClient </strong>project contains a Console Application that can be used to test the library</p>

<h1>ServiceBusExtensions Library</h1>

<p>The following table contains the code of the QueueClientExtensions class. The code for the MessageSenderExtensions and TopicClientExtensions classes is very similar, so I will omit it for simplicity.</p>

<div class="scriptcode">
  <div class="pluginEditHolder" plugincommand="mceScriptCode">
    <div id="scid:57F11A72-B0E5-49c7-9094-E3A15BD5B5E6:2975d4fc-7f4f-44f4-b6e8-0e5ae7bce521" class="wlWriterEditableSmartContent" style="margin: 0px; padding: 0px; float: none; display: inline;"><pre style="background-color:#FFFFFF;white-space:-moz-pre-wrap; white-space: -pre-wrap; white-space: -o-pre-wrap; white-space: pre-wrap; word-wrap: break-word;overflow: auto;"><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Using Directives</span><span style="color: #000000;">
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Collections.Generic;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Diagnostics;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Linq;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> Microsoft.ServiceBus.Messaging;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Threading.Tasks;
</span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

</span><span style="color: #0000FF;">namespace</span><span style="color: #000000;"> Microsoft.AzureCat.ServiceBusExtensions
{
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;summary&gt;</span><span style="color: #008000;">
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> This class contains extensions methods for the QueueClient class
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;/summary&gt;</span><span style="color: #808080;">
</span><span style="color: #000000;">  </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">class</span><span style="color: #000000;"> QueueClientClientExtensions
  {
    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Private Constants</span><span style="color: #000000;">
    </span><span style="color: #008000;">//</span><span style="color: #008000;">*******************************
    </span><span style="color: #008000;">//</span><span style="color: #008000;"> Formats
    </span><span style="color: #008000;">//</span><span style="color: #008000;">*******************************</span><span style="color: #008000;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> BrokeredMessageListCannotBeNullOrEmpty </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">The brokeredMessageEnumerable parameter cannot be null or empty.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> SendPartitionedBatchFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">[QueueClient.SendPartitionedBatch] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> SendPartitionedBatchAsyncFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">[QueueClient.SendPartitionedBatchAsync] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Public Methods</span><span style="color: #000000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> Sends a set of brokered messages (for batch processing). 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> If the batch size is greater than the maximum batch size, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> the method partitions the original batch into multiple batches, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> each smaller in size than the maximum batch size.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;/summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="queueClient"&gt;</span><span style="color: #008000;">The current QueueClient object.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="brokeredMessageEnumerable"&gt;</span><span style="color: #008000;">The collection of brokered messages to send.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="trace"&gt;</span><span style="color: #008000;">true to cause a message to be written; otherwise, false.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;returns&gt;</span><span style="color: #008000;">The asynchronous operation.</span><span style="color: #808080;">&lt;/returns&gt;</span><span style="color: #808080;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> async </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> Task SendPartitionedBatchAsync(</span><span style="color: #0000FF;">this</span><span style="color: #000000;"> QueueClient queueClient, IEnumerable</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> brokeredMessageEnumerable, </span><span style="color: #0000FF;">bool</span><span style="color: #000000;"> trace </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">false</span><span style="color: #000000;">)
    {
      var brokeredMessageList </span><span style="color: #000000;">=</span><span style="color: #000000;"> brokeredMessageEnumerable </span><span style="color: #0000FF;">as</span><span style="color: #000000;"> IList</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> </span><span style="color: #000000;">??</span><span style="color: #000000;"> brokeredMessageEnumerable.ToList();
      </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (brokeredMessageEnumerable </span><span style="color: #000000;">==</span><span style="color: #000000;"> </span><span style="color: #0000FF;">null</span><span style="color: #000000;"> </span><span style="color: #000000;">||</span><span style="color: #000000;"> </span><span style="color: #000000;">!</span><span style="color: #000000;">brokeredMessageList.Any())
      {
        </span><span style="color: #0000FF;">throw</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> ArgumentNullException(BrokeredMessageListCannotBeNullOrEmpty);
      }

      var batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;">();
      </span><span style="color: #0000FF;">long</span><span style="color: #000000;"> batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">;

      </span><span style="color: #0000FF;">foreach</span><span style="color: #000000;"> (var brokeredMessage </span><span style="color: #0000FF;">in</span><span style="color: #000000;"> brokeredMessageList)
      {
        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> ((batchSize </span><span style="color: #000000;">+</span><span style="color: #000000;"> brokeredMessage.Size) </span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> Constants.MaxBathSizeInBytes)
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          await queueClient.SendBatchAsync(batchList);
          Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));

          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Initialize a new batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> { brokeredMessage };
          batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> brokeredMessage.Size;
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Add the BrokeredMessage to the current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList.Add(brokeredMessage);
          batchSize </span><span style="color: #000000;">+=</span><span style="color: #000000;"> brokeredMessage.Size;
        }
      }
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> The final batch is sent outside of the loop</span><span style="color: #008000;">
</span><span style="color: #000000;">      await queueClient.SendBatchAsync(batchList);
      Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));
    }

    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> Sends a set of brokered messages (for batch processing). 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> If the batch size is greater than the maximum batch size, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> the method partitions the original batch into multiple batches, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> each smaller in size than the maximum batch size.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;/summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="queueClient"&gt;</span><span style="color: #008000;">The current QueueClient object.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="brokeredMessageEnumerable"&gt;</span><span style="color: #008000;">The collection of brokered messages to send.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="trace"&gt;</span><span style="color: #008000;">true to cause a message to be written; otherwise, false.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #808080;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">void</span><span style="color: #000000;"> SendPartitionedBatch(</span><span style="color: #0000FF;">this</span><span style="color: #000000;"> QueueClient queueClient, IEnumerable</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> brokeredMessageEnumerable, </span><span style="color: #0000FF;">bool</span><span style="color: #000000;"> trace </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">false</span><span style="color: #000000;">)
    {
      var brokeredMessageList </span><span style="color: #000000;">=</span><span style="color: #000000;"> brokeredMessageEnumerable </span><span style="color: #0000FF;">as</span><span style="color: #000000;"> IList</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> </span><span style="color: #000000;">??</span><span style="color: #000000;"> brokeredMessageEnumerable.ToList();
      </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (brokeredMessageEnumerable </span><span style="color: #000000;">==</span><span style="color: #000000;"> </span><span style="color: #0000FF;">null</span><span style="color: #000000;"> </span><span style="color: #000000;">||</span><span style="color: #000000;"> </span><span style="color: #000000;">!</span><span style="color: #000000;">brokeredMessageList.Any())
      {
        </span><span style="color: #0000FF;">throw</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> ArgumentNullException(BrokeredMessageListCannotBeNullOrEmpty);
      }

      var batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;">();
      </span><span style="color: #0000FF;">long</span><span style="color: #000000;"> batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">;

      </span><span style="color: #0000FF;">foreach</span><span style="color: #000000;"> (var brokeredMessage </span><span style="color: #0000FF;">in</span><span style="color: #000000;"> brokeredMessageList)
      {
        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> ((batchSize </span><span style="color: #000000;">+</span><span style="color: #000000;"> brokeredMessage.Size) </span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> Constants.MaxBathSizeInBytes)
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          queueClient.SendBatch(batchList);
          Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));

          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Initialize a new batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> { brokeredMessage };
          batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> brokeredMessage.Size;
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Add the BrokeredMessage to the current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList.Add(brokeredMessage);
          batchSize </span><span style="color: #000000;">+=</span><span style="color: #000000;"> brokeredMessage.Size;
        }
      }
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> The final batch is sent outside of the loop</span><span style="color: #008000;">
</span><span style="color: #000000;">      queueClient.SendBatch(batchList);
      Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));
    }
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">
  }
}</span></pre><!-- Code inserted with Steve Dunn's Windows Live Writer Code Formatter Plugin.  http://dunnhq.com --></div>
  </div>
</div>

<p>The following table contains the code of the EventHubClientExtensions class. <strong> Note</strong>: all the event data sent in a batch using the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventhubclient.sendbatch.aspx">EventHubClient.SendBatch</a> or <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventhubclient.sendbatchasync.aspx">EventHubClient.SendBatchAsync</a> methods need to have the same <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventdata.partitionkey.aspx">PartitionKey</a>. In fact, when using one of these methods, all the event data contained in the batch are insersted in the same partition of the event hub by the Event Hub message broker.<span>&#160;</span>Hence, they need to share the same value in the PartitionKey property as the latter is used to determine to which partition to send event data.</p>

<div id="scid:57F11A72-B0E5-49c7-9094-E3A15BD5B5E6:9bbe6ad5-c782-4bc1-a445-0a98f43bb352" class="wlWriterEditableSmartContent" style="margin: 0px; padding: 0px; float: none; display: inline;"><pre style="background-color:#FFFFFF;white-space:-moz-pre-wrap; white-space: -pre-wrap; white-space: -o-pre-wrap; white-space: pre-wrap; word-wrap: break-word;overflow: auto;"><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Copyright</span><span style="color: #000000;">
</span><span style="color: #008000;">//</span><span style="color: #008000;">=======================================================================================
</span><span style="color: #008000;">//</span><span style="color: #008000;"> Microsoft Azure Customer Advisory Team  
</span><span style="color: #008000;">//</span><span style="color: #008000;">
</span><span style="color: #008000;">//</span><span style="color: #008000;"> This sample is supplemental to the technical guidance published on the community
</span><span style="color: #008000;">//</span><span style="color: #008000;"> blog at </span><span style="color: #008000; text-decoration: underline;">http://blogs.msdn.com/b/paolos/.</span><span style="color: #008000;"> 
</span><span style="color: #008000;">//</span><span style="color: #008000;"> 
</span><span style="color: #008000;">//</span><span style="color: #008000;"> Author: Paolo Salvatori
</span><span style="color: #008000;">//</span><span style="color: #008000;">=======================================================================================
</span><span style="color: #008000;">//</span><span style="color: #008000;"> Copyright © 2015 Microsoft Corporation. All rights reserved.
</span><span style="color: #008000;">//</span><span style="color: #008000;"> 
</span><span style="color: #008000;">//</span><span style="color: #008000;"> THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
</span><span style="color: #008000;">//</span><span style="color: #008000;"> EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
</span><span style="color: #008000;">//</span><span style="color: #008000;"> MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
</span><span style="color: #008000;">//</span><span style="color: #008000;">=======================================================================================</span><span style="color: #008000;">
</span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

</span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Using Directives</span><span style="color: #000000;">
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Collections.Generic;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Diagnostics;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Linq;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> Microsoft.ServiceBus.Messaging; 
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Threading.Tasks;
</span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

</span><span style="color: #0000FF;">namespace</span><span style="color: #000000;"> Microsoft.AzureCat.ServiceBusExtensions
{
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;summary&gt;</span><span style="color: #008000;">
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> This class contains extensions methods for the EventHubClient class
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;/summary&gt;</span><span style="color: #808080;">
</span><span style="color: #000000;">  </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">class</span><span style="color: #000000;"> EventHubClientExtensions
  {
    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Private Constants</span><span style="color: #000000;">
    </span><span style="color: #008000;">//</span><span style="color: #008000;">*******************************
    </span><span style="color: #008000;">//</span><span style="color: #008000;"> Formats
    </span><span style="color: #008000;">//</span><span style="color: #008000;">*******************************</span><span style="color: #008000;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> EventDataListCannotBeNullOrEmpty </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">The eventDataEnumerable parameter cannot be null or empty.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> SendPartitionedBatchFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">[EventHubClient.SendPartitionedBatch] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> SendPartitionedBatchAsyncFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">[EventHubClient.SendPartitionedBatchAsync] Batch Sent: BatchSizeInBytes=[{0}] MessageCount=[{1}]</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Public Methods</span><span style="color: #000000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> Asynchronously sends a batch of event data to the same partition.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> All the event data in the batch need to have the same value in the Partitionkey property.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> If the batch size is greater than the maximum batch size, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> the method partitions the original batch into multiple batches, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> each smaller in size than the maximum batch size.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;/summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="eventHubClient"&gt;</span><span style="color: #008000;">The current EventHubClient object.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="eventDataEnumerable"&gt;</span><span style="color: #008000;">An IEnumerable object containing event data instances.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="trace"&gt;</span><span style="color: #008000;">true to cause a message to be written; otherwise, false.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;returns&gt;</span><span style="color: #008000;">The asynchronous operation.</span><span style="color: #808080;">&lt;/returns&gt;</span><span style="color: #808080;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> async </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> Task SendPartitionedBatchAsync(</span><span style="color: #0000FF;">this</span><span style="color: #000000;"> EventHubClient eventHubClient, IEnumerable</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> eventDataEnumerable, </span><span style="color: #0000FF;">bool</span><span style="color: #000000;"> trace </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">false</span><span style="color: #000000;">)
    {
      var eventDataList </span><span style="color: #000000;">=</span><span style="color: #000000;"> eventDataEnumerable </span><span style="color: #0000FF;">as</span><span style="color: #000000;"> IList</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> </span><span style="color: #000000;">??</span><span style="color: #000000;"> eventDataEnumerable.ToList();
      </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (eventDataEnumerable </span><span style="color: #000000;">==</span><span style="color: #000000;"> </span><span style="color: #0000FF;">null</span><span style="color: #000000;"> </span><span style="color: #000000;">||</span><span style="color: #000000;"> </span><span style="color: #000000;">!</span><span style="color: #000000;">eventDataList.Any())
      {
        </span><span style="color: #0000FF;">throw</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> ArgumentNullException(EventDataListCannotBeNullOrEmpty);
      }

      var batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;">();
      </span><span style="color: #0000FF;">long</span><span style="color: #000000;"> batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">;

      </span><span style="color: #0000FF;">foreach</span><span style="color: #000000;"> (var eventData </span><span style="color: #0000FF;">in</span><span style="color: #000000;"> eventDataList)
      {
        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> ((batchSize </span><span style="color: #000000;">+</span><span style="color: #000000;"> eventData.SerializedSizeInBytes) </span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> Constants.MaxBathSizeInBytes)
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          await eventHubClient.SendBatchAsync(batchList);
          Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));

          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Initialize a new batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> { eventData };
          batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> eventData.SerializedSizeInBytes;
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Add the EventData to the current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList.Add(eventData);
          batchSize </span><span style="color: #000000;">+=</span><span style="color: #000000;"> eventData.SerializedSizeInBytes;
        }
      }
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> The final batch is sent outside of the loop</span><span style="color: #008000;">
</span><span style="color: #000000;">      await eventHubClient.SendBatchAsync(batchList);
      Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchAsyncFormat, batchSize, batchList.Count));
    }

    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> Asynchronously sends a batch of event data to the same partition.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> All the event data in the batch need to have the same value in the Partitionkey property.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> If the batch size is greater than the maximum batch size, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> the method partitions the original batch into multiple batches, 
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> each smaller in size than the maximum batch size.
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;/summary&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="eventHubClient"&gt;</span><span style="color: #008000;">The current EventHubClient object.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="eventDataEnumerable"&gt;</span><span style="color: #008000;">An IEnumerable object containing event data instances.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #008000;">
    </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;param name="trace"&gt;</span><span style="color: #008000;">true to cause a message to be written; otherwise, false.</span><span style="color: #808080;">&lt;/param&gt;</span><span style="color: #808080;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">void</span><span style="color: #000000;"> SendPartitionedBatch(</span><span style="color: #0000FF;">this</span><span style="color: #000000;"> EventHubClient eventHubClient, IEnumerable</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> eventDataEnumerable, </span><span style="color: #0000FF;">bool</span><span style="color: #000000;"> trace </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">false</span><span style="color: #000000;">)
    {
      var eventDataList </span><span style="color: #000000;">=</span><span style="color: #000000;"> eventDataEnumerable </span><span style="color: #0000FF;">as</span><span style="color: #000000;"> IList</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> </span><span style="color: #000000;">??</span><span style="color: #000000;"> eventDataEnumerable.ToList();
      </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (eventDataEnumerable </span><span style="color: #000000;">==</span><span style="color: #000000;"> </span><span style="color: #0000FF;">null</span><span style="color: #000000;"> </span><span style="color: #000000;">||</span><span style="color: #000000;"> </span><span style="color: #000000;">!</span><span style="color: #000000;">eventDataList.Any())
      {
        </span><span style="color: #0000FF;">throw</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> ArgumentNullException(EventDataListCannotBeNullOrEmpty);
      }

      var batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;">();
      </span><span style="color: #0000FF;">long</span><span style="color: #000000;"> batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">;

      </span><span style="color: #0000FF;">foreach</span><span style="color: #000000;"> (var eventData </span><span style="color: #0000FF;">in</span><span style="color: #000000;"> eventDataList)
      {
        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> ((batchSize </span><span style="color: #000000;">+</span><span style="color: #000000;"> eventData.SerializedSizeInBytes) </span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> Constants.MaxBathSizeInBytes)
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          eventHubClient.SendBatch(batchList);
          Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));

          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Initialize a new batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> { eventData };
          batchSize </span><span style="color: #000000;">=</span><span style="color: #000000;"> eventData.SerializedSizeInBytes;
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Add the EventData to the current batch</span><span style="color: #008000;">
</span><span style="color: #000000;">          batchList.Add(eventData);
          batchSize </span><span style="color: #000000;">+=</span><span style="color: #000000;"> eventData.SerializedSizeInBytes;
        }
      }
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> The final batch is sent outside of the loop</span><span style="color: #008000;">
</span><span style="color: #000000;">      eventHubClient.SendBatch(batchList);
      Trace.WriteLineIf(trace, </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SendPartitionedBatchFormat, batchSize, batchList.Count));
    } 
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">
  }
}</span></pre><!-- Code inserted with Steve Dunn's Windows Live Writer Code Formatter Plugin.  http://dunnhq.com --></div>

<h1>
  <div class="scriptcode">
    <div class="pluginEditHolder" plugincommand="mceScriptCode">
      <p>TestClient</p>
    </div>
  </div>
</h1>

<p>The following picture shows the <strong>TestClient </strong>console application that can be used to test each extension method defined by the <strong>ServiceBusExtensions </strong>library.</p>

<p align="center"><a href="$TestClient[3].png"><img title="TestClient" style="display: inline; background-image: none;" border="0" alt="TestClient" src="$TestClient_thumb[1].png" width="804" height="424" /></a></p>

<p>In the <strong>appSettings </strong>section of the configuration file you can define the following settings:</p>

<ul>
  <li><strong>connectionString</strong>: the Service Bus namespace connectionstring. </li>

  <li><strong>messageSizeInBytes</strong>: the size of individual BrokeredMessage and EventData messages. </li>

  <li><strong>messageCountInBatch</strong>: the number of messages in a batch. </li>
</ul>

<p>Upon start, the client application creates the following entities in the target Service Bus namespace, if they don't already exist.</p>

<ul>
  <li><strong>batchtestqueue</strong>: this this is the queue used to test the extensions method contained in the QueueClientExtensions and MessageSenderExtensions classes. </li>

  <li><strong>batchtesttopic</strong>: this is the topic used to test the extensions method contained in the TopicClientExtensions class. </li>

  <li><strong>batcheventhub</strong>: this is the event hub used to test the extensions method contained in the EventHubClientExtensions class </li>
</ul>

<p>Then, the user can use the menu shown by the application to select one of the tests. Each test tries to use the original SendBatchAsync method exposed by each of the <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.messagesender.aspx?f=255&amp;MSPPError=-2147217396">essageSender</a><span>, </span><a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.queueclient.aspx">QueueClient</a><span>,</span><a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.topicclient.aspx">TopicClient</a><span>, and </span><a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.eventhubclient.aspx">EventHubClient</a> classes. If the batch size is greater than the maximum allowed size, the method call will throw a <a href="https://msdn.microsoft.com/en-us/library/microsoft.servicebus.messaging.messagesizeexceededexception.aspx">MessageSizeExceededException</a><span>. The SendPartitionedBatchAsync method instead will split the original batch into one or multiple batches, each smaller than the maximum allowed size, and will send them in the proper order to the target entity.</span></p>

<p><span>For your convenience, the following tables includes the code of the <strong>TestClient </strong>console application.</span></p>

<div class="scriptcode">
  <div id="scid:57F11A72-B0E5-49c7-9094-E3A15BD5B5E6:9a98a3a0-1552-4a3f-900a-c048165887af" class="wlWriterEditableSmartContent" style="margin: 0px; padding: 0px; float: none; display: inline;"><pre style="background-color:#FFFFFF;white-space:-moz-pre-wrap; white-space: -pre-wrap; white-space: -o-pre-wrap; white-space: pre-wrap; word-wrap: break-word;overflow: auto;"><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Using Directives</span><span style="color: #000000;">
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Collections.Generic;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Configuration;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Diagnostics;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Globalization;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.IO;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Runtime.CompilerServices;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Text;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> System.Threading.Tasks;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> Microsoft.ServiceBus;
</span><span style="color: #0000FF;">using</span><span style="color: #000000;"> Microsoft.ServiceBus.Messaging;

</span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

</span><span style="color: #0000FF;">namespace</span><span style="color: #000000;"> Microsoft.AzureCat.ServiceBusExtensions.TestClient
{
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;summary&gt;</span><span style="color: #008000;">
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> This class can be used to test the extensions methods defines in the ServiceBusExtensions library.
  </span><span style="color: #808080;">///</span><span style="color: #008000;"> </span><span style="color: #808080;">&lt;/summary&gt;</span><span style="color: #808080;">
</span><span style="color: #000000;">  </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> </span><span style="color: #0000FF;">class</span><span style="color: #000000;"> Program
  {
    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Private Constants</span><span style="color: #000000;">
    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************
    </span><span style="color: #008000;">//</span><span style="color: #008000;"> Configuration Parameters
    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************</span><span style="color: #008000;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> ConnectionString </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">connectionString</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> MessageSizeInBytes </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">messageSizeInBytes</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> MessageCountInBatch </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">messageCountInBatch</span><span style="color: #800000;">"</span><span style="color: #000000;">;

    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************
    </span><span style="color: #008000;">//</span><span style="color: #008000;"> Entities
    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************</span><span style="color: #008000;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> QueueName </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">batchtestqueue</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> TopicName </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">batchtesttopic</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> SubscriptionName </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">auditing</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> EventHubName </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">batchtesteventhub</span><span style="color: #800000;">"</span><span style="color: #000000;">;

    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************
    </span><span style="color: #008000;">//</span><span style="color: #008000;"> Default Values
    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************</span><span style="color: #008000;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> DefaultMessageCountInBatch </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">100</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> DefaultMessageSizeInBytes </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">16384</span><span style="color: #000000;">;

    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************
    </span><span style="color: #008000;">//</span><span style="color: #008000;"> Formats
    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************</span><span style="color: #008000;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> ParameterFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">{0}: [{1}]</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> PressKeyToExit </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Press a key to exit.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> MenuChoiceFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Select a numeric key between 1 and {0}</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> ConnectionStringCannotBeNull </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">The Service Bus connection string has not been defined in the configuration file.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> QueueCreatedFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Queue [{0}] successfully created.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> TopicCreatedFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Topic [{0}] successfully created.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> SubscriptionCreatedFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Subscription [{0}] successfully created.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> EventHubCreatedFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Event Hub [{0}] successfully created.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> QueueAlreadyExistsFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Queue [{0}] already exists.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> TopicAlreadyExistsFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Topic [{0}] already exists.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> SubscriptionAlreadyExistsFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Subscription [{0}] already exists.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> EventHubAlreadyExistsFormat </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Event Hub [{0}] already exists.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingMessageSenderSendBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling MessageSender.SendBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> MessageSenderSendBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">MessageSender.SendBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingMessageSenderSendPartitionedBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling MessageSender.SendPartitionedBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> MessageSenderSendPartitionedBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">MessageSender.SendPartitionedBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingQueueClientSendBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling QueueClient.SendBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> QueueClientSendBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">QueueClient.SendBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingQueueClientSendPartitionedBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling QueueClient.SendPartitionedBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> QueueClientSendPartitionedBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">QueueClient.SendPartitionedBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingTopicClientSendBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling TopicClient.SendBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> TopicClientSendBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">TopicClient.SendBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingTopicClientSendPartitionedBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling TopicClient.SendPartitionedBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> TopicClientSendPartitionedBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">TopicClient.SendPartitionedBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingEventHubClientSendBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling EventHubClient.SendBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> EventHubClientSendBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">EventHubClient.SendBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> CallingEventHubClientSendPartitionedBatchAsync </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Calling EventHubClient.SendPartitionedBatchAsync...</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> EventHubClientSendPartitionedBatchAsyncCalled </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">EventHubClient.SendPartitionedBatchAsync called.</span><span style="color: #800000;">"</span><span style="color: #000000;">;

    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************
    </span><span style="color: #008000;">//</span><span style="color: #008000;"> Menu Items
    </span><span style="color: #008000;">//</span><span style="color: #008000;">***************************</span><span style="color: #008000;">
</span><span style="color: #000000;">    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> MessageSenderSendPartitionedBatchAsyncTest </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">MessageSender.SendPartitionedBatchAsync Test</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> QueueClientSendPartitionedBatchAsyncTest </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">QueueClient.SendPartitionedBatchAsync Test</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> TopicClientSendPartitionedBatchAsyncTest </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">TopicClient.SendPartitionedBatchAsync Test</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> EventHubClientSendPartitionedBatchAsyncTest </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">EventHubClient.SendPartitionedBatchAsync Test</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">const</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> Exit </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Exit</span><span style="color: #800000;">"</span><span style="color: #000000;">;
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Private Static Fields</span><span style="color: #000000;">
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> connectionString;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> messageSizeInBytes;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> messageCountInBatch;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> MessagingFactory messagingFactory;
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">readonly</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #0000FF;">string</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> menuItemList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #0000FF;">string</span><span style="color: #000000;">&gt;</span><span style="color: #000000;">
    {
      MessageSenderSendPartitionedBatchAsyncTest ,
      QueueClientSendPartitionedBatchAsyncTest,
      TopicClientSendPartitionedBatchAsyncTest,
      EventHubClientSendPartitionedBatchAsyncTest,
      Exit
    }; 
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Main Method</span><span style="color: #000000;">
    </span><span style="color: #0000FF;">public</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">void</span><span style="color: #000000;"> Main()
    {
      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (ReadConfiguration() </span><span style="color: #000000;">&amp;&amp;</span><span style="color: #000000;"> CreateEntitiesAsync().Result)
        {
          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Add ConsoleTraceListener</span><span style="color: #008000;">
</span><span style="color: #000000;">          Trace.Listeners.Add(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> ConsoleTraceListener());

          </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create MessagingFactory object</span><span style="color: #008000;">
</span><span style="color: #000000;">          messagingFactory </span><span style="color: #000000;">=</span><span style="color: #000000;"> MessagingFactory.CreateFromConnectionString(connectionString);

          </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> key;
          </span><span style="color: #0000FF;">while</span><span style="color: #000000;"> ((key </span><span style="color: #000000;">=</span><span style="color: #000000;"> ShowMenu()) </span><span style="color: #000000;">!=</span><span style="color: #000000;"> menuItemList.Count </span><span style="color: #000000;">-</span><span style="color: #000000;"> </span><span style="color: #800080;">1</span><span style="color: #000000;">)
          {
            </span><span style="color: #0000FF;">switch</span><span style="color: #000000;"> (menuItemList[key])
            {
              </span><span style="color: #0000FF;">case</span><span style="color: #000000;"> MessageSenderSendPartitionedBatchAsyncTest:
                </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test MessageSender.SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">                MessageSenderTest().Wait();
                </span><span style="color: #0000FF;">break</span><span style="color: #000000;">;
              </span><span style="color: #0000FF;">case</span><span style="color: #000000;"> QueueClientSendPartitionedBatchAsyncTest:
                </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test QueueClient.SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">                QueueClientTest().Wait();
                </span><span style="color: #0000FF;">break</span><span style="color: #000000;">;
              </span><span style="color: #0000FF;">case</span><span style="color: #000000;"> TopicClientSendPartitionedBatchAsyncTest:
                </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test TopicClient.SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">                TopicClientTest().Wait();
                </span><span style="color: #0000FF;">break</span><span style="color: #000000;">;
              </span><span style="color: #0000FF;">case</span><span style="color: #000000;"> EventHubClientSendPartitionedBatchAsyncTest:
                </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test EventHubClient.SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">                EventHubClientTest().Wait();
                </span><span style="color: #0000FF;">break</span><span style="color: #000000;">;
              </span><span style="color: #0000FF;">case</span><span style="color: #000000;"> Exit:
                </span><span style="color: #0000FF;">break</span><span style="color: #000000;">;
            }
          }
        }
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }
      PrintMessage(PressKeyToExit);
      Console.ReadLine();
    } 
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">

    </span><span style="color: #0000FF;">#region</span><span style="color: #000000;"> Private Static Methods</span><span style="color: #000000;">
    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> async </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> Task MessageSenderTest()
    {
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create MessageSender object</span><span style="color: #008000;">
</span><span style="color: #000000;">      var messageSender </span><span style="color: #000000;">=</span><span style="color: #000000;"> await messagingFactory.CreateMessageSenderAsync(QueueName);

      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test MessageSender.SendBatchAsync: if the batch size is greater than the max batch size
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> the method throws a  MessageSizeExceededException</span><span style="color: #008000;">
</span><span style="color: #000000;">      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        PrintMessage(CallingMessageSenderSendBatchAsync);
        await messageSender.SendBatchAsync(CreateBrokeredMessageBatch());
        PrintMessage(MessageSenderSendBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }

      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send the batch using the SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">        PrintMessage(CallingMessageSenderSendPartitionedBatchAsync);
        await messageSender.SendPartitionedBatchAsync(CreateBrokeredMessageBatch(), </span><span style="color: #0000FF;">true</span><span style="color: #000000;">);
        PrintMessage(MessageSenderSendPartitionedBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> async </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> Task QueueClientTest()
    {
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create QueueClient object</span><span style="color: #008000;">
</span><span style="color: #000000;">      var queueClient </span><span style="color: #000000;">=</span><span style="color: #000000;"> messagingFactory.CreateQueueClient(QueueName);

      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test QueueClient.SendBatchAsync: if the batch size is greater than the max batch size
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> the method throws a  MessageSizeExceededException</span><span style="color: #008000;">
</span><span style="color: #000000;">      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        PrintMessage(CallingQueueClientSendBatchAsync);
        await queueClient.SendBatchAsync(CreateBrokeredMessageBatch());
        PrintMessage(QueueClientSendBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }

      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send the batch using the SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">        PrintMessage(CallingQueueClientSendPartitionedBatchAsync);
        await queueClient.SendPartitionedBatchAsync(CreateBrokeredMessageBatch(), </span><span style="color: #0000FF;">true</span><span style="color: #000000;">);
        PrintMessage(QueueClientSendPartitionedBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> async </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> Task TopicClientTest()
    {
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create TopicClient object</span><span style="color: #008000;">
</span><span style="color: #000000;">      var topicClient </span><span style="color: #000000;">=</span><span style="color: #000000;"> messagingFactory.CreateTopicClient(TopicName);

      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test TopicClient.SendBatchAsync: if the batch size is greater than the max batch size
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> the method throws a  MessageSizeExceededException</span><span style="color: #008000;">
</span><span style="color: #000000;">      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        PrintMessage(CallingTopicClientSendBatchAsync);
        await topicClient.SendBatchAsync(CreateBrokeredMessageBatch());
        PrintMessage(TopicClientSendBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }

      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send the batch using the SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">        PrintMessage(CallingTopicClientSendPartitionedBatchAsync);
        await topicClient.SendPartitionedBatchAsync(CreateBrokeredMessageBatch(), </span><span style="color: #0000FF;">true</span><span style="color: #000000;">);
        PrintMessage(TopicClientSendPartitionedBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> async </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> Task EventHubClientTest()
    {
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create EventHubClient object</span><span style="color: #008000;">
</span><span style="color: #000000;">      var eventHubClient </span><span style="color: #000000;">=</span><span style="color: #000000;"> EventHubClient.CreateFromConnectionString(connectionString, EventHubName);

      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Test EventHubClient.SendBatchAsync: if the batch size is greater than the max batch size
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> the method throws a  MessageSizeExceededException</span><span style="color: #008000;">
</span><span style="color: #000000;">      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        PrintMessage(CallingEventHubClientSendBatchAsync);
        await eventHubClient.SendBatchAsync(CreateEventDataBatch());
        PrintMessage(EventHubClientSendBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }

      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Send the batch using the SendPartitionedBatchAsync method</span><span style="color: #008000;">
</span><span style="color: #000000;">        PrintMessage(CallingEventHubClientSendPartitionedBatchAsync);
        await eventHubClient.SendPartitionedBatchAsync(CreateEventDataBatch(), </span><span style="color: #0000FF;">true</span><span style="color: #000000;">);
        PrintMessage(EventHubClientSendPartitionedBatchAsyncCalled);
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> IEnumerable</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> CreateBrokeredMessageBatch()
    {
      var messageList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">BrokeredMessage</span><span style="color: #000000;">&gt;</span><span style="color: #000000;">();
      </span><span style="color: #0000FF;">for</span><span style="color: #000000;"> (var i </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">; i </span><span style="color: #000000;">&lt;</span><span style="color: #000000;"> messageCountInBatch; i</span><span style="color: #000000;">++</span><span style="color: #000000;">)
      {
        messageList.Add(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> BrokeredMessage(Encoding.UTF8.GetBytes(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;">(</span><span style="color: #800000;">'</span><span style="color: #800000;">A</span><span style="color: #800000;">'</span><span style="color: #000000;">, messageSizeInBytes))));
      }
      </span><span style="color: #0000FF;">return</span><span style="color: #000000;"> messageList;
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> IEnumerable</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> CreateEventDataBatch()
    {
      var messageList </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> List</span><span style="color: #000000;">&lt;</span><span style="color: #000000;">EventData</span><span style="color: #000000;">&gt;</span><span style="color: #000000;">();
      </span><span style="color: #0000FF;">for</span><span style="color: #000000;"> (var i </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">; i </span><span style="color: #000000;">&lt;</span><span style="color: #000000;"> messageCountInBatch; i</span><span style="color: #000000;">++</span><span style="color: #000000;">)
      {
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Note: the partition key in this sample is null.
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> it's mandatory that all event data in a batch have the same PartitionKey</span><span style="color: #008000;">
</span><span style="color: #000000;">        messageList.Add(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> EventData(Encoding.UTF8.GetBytes(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;">(</span><span style="color: #800000;">'</span><span style="color: #800000;">A</span><span style="color: #800000;">'</span><span style="color: #000000;">, messageSizeInBytes))));
      }
      </span><span style="color: #0000FF;">return</span><span style="color: #000000;"> messageList;
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> async </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> Task</span><span style="color: #000000;">&lt;</span><span style="color: #0000FF;">bool</span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> CreateEntitiesAsync()
    {
      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create NamespaceManeger object</span><span style="color: #008000;">
</span><span style="color: #000000;">        var namespaceManager </span><span style="color: #000000;">=</span><span style="color: #000000;"> NamespaceManager.CreateFromConnectionString(connectionString);
        
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create test queue</span><span style="color: #008000;">
</span><span style="color: #000000;">        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (</span><span style="color: #000000;">!</span><span style="color: #000000;">await namespaceManager.QueueExistsAsync(QueueName))
        {
          await namespaceManager.CreateQueueAsync(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> QueueDescription(QueueName)
          {
            EnableBatchedOperations </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">,
            EnableExpress </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">,
            EnablePartitioning </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">,
            EnableDeadLetteringOnMessageExpiration </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">
          });
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(QueueCreatedFormat, QueueName));
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(QueueAlreadyExistsFormat, QueueName));
        }
        
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create test topic</span><span style="color: #008000;">
</span><span style="color: #000000;">        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (</span><span style="color: #000000;">!</span><span style="color: #000000;">await namespaceManager.TopicExistsAsync(TopicName))
        {
          await namespaceManager.CreateTopicAsync(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> TopicDescription(TopicName)
          {
            EnableBatchedOperations </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">,
            EnableExpress </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">,
            EnablePartitioning </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">,
          });
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(TopicCreatedFormat, TopicName));
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(TopicAlreadyExistsFormat, TopicName));
        }
        
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create test subscription</span><span style="color: #008000;">
</span><span style="color: #000000;">        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (</span><span style="color: #000000;">!</span><span style="color: #000000;">await namespaceManager.SubscriptionExistsAsync(TopicName, SubscriptionName))
        {
          await namespaceManager.CreateSubscriptionAsync(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> SubscriptionDescription(TopicName, SubscriptionName)
          {
            EnableBatchedOperations </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">
          }, </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> RuleDescription(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> TrueFilter()));
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SubscriptionCreatedFormat, SubscriptionName));
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(SubscriptionAlreadyExistsFormat, SubscriptionName));
        }

        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Create test event hub</span><span style="color: #008000;">
</span><span style="color: #000000;">        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (</span><span style="color: #000000;">!</span><span style="color: #000000;">await namespaceManager.EventHubExistsAsync(EventHubName))
        {
          await namespaceManager.CreateEventHubAsync(</span><span style="color: #0000FF;">new</span><span style="color: #000000;"> EventHubDescription(EventHubName)
          {
            PartitionCount </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">16</span><span style="color: #000000;">,
            MessageRetentionInDays </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">1</span><span style="color: #000000;">
          });
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(EventHubCreatedFormat, EventHubName));
        }
        </span><span style="color: #0000FF;">else</span><span style="color: #000000;">
        {
          PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(EventHubAlreadyExistsFormat, EventHubName));
        }
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
        </span><span style="color: #0000FF;">return</span><span style="color: #000000;"> </span><span style="color: #0000FF;">false</span><span style="color: #000000;">;
      }
      </span><span style="color: #0000FF;">return</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">;
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> ShowMenu()
    {
      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Print Menu Header</span><span style="color: #008000;">
</span><span style="color: #000000;">      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">[</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Yellow;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">Menu</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.WriteLine(</span><span style="color: #800000;">"</span><span style="color: #800000;">]</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ResetColor();

      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Print Menu Items</span><span style="color: #008000;">
</span><span style="color: #000000;">      </span><span style="color: #0000FF;">for</span><span style="color: #000000;"> (var i </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">; i </span><span style="color: #000000;">&lt;</span><span style="color: #000000;"> menuItemList.Count; i</span><span style="color: #000000;">++</span><span style="color: #000000;">)
      {
        Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
        Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">[</span><span style="color: #800000;">"</span><span style="color: #000000;">);
        Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Yellow;
        Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">{0}</span><span style="color: #800000;">"</span><span style="color: #000000;">, i </span><span style="color: #000000;">+</span><span style="color: #000000;"> </span><span style="color: #800080;">1</span><span style="color: #000000;">);
        Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
        Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">]</span><span style="color: #800000;">"</span><span style="color: #000000;">);
        Console.ResetColor();
        Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">: </span><span style="color: #800000;">"</span><span style="color: #000000;">);
        Console.WriteLine(menuItemList[i]);
        Console.ResetColor();
      }

      </span><span style="color: #008000;">//</span><span style="color: #008000;"> Select an option</span><span style="color: #008000;">
</span><span style="color: #000000;">      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">[</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Yellow;
      Console.Write(MenuChoiceFormat, menuItemList.Count);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">]</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ResetColor();
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">: </span><span style="color: #800000;">"</span><span style="color: #000000;">);

      var key </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">'</span><span style="color: #800000;">a</span><span style="color: #800000;">'</span><span style="color: #000000;">;
      </span><span style="color: #0000FF;">while</span><span style="color: #000000;"> (key </span><span style="color: #000000;">&lt;</span><span style="color: #000000;"> </span><span style="color: #800000;">'</span><span style="color: #800000;">1</span><span style="color: #800000;">'</span><span style="color: #000000;"> </span><span style="color: #000000;">||</span><span style="color: #000000;"> key </span><span style="color: #000000;">&gt;</span><span style="color: #000000;"> </span><span style="color: #800000;">'</span><span style="color: #800000;">9</span><span style="color: #800000;">'</span><span style="color: #000000;">)
      {
        key </span><span style="color: #000000;">=</span><span style="color: #000000;"> Console.ReadKey(</span><span style="color: #0000FF;">true</span><span style="color: #000000;">).KeyChar;
      }
      Console.WriteLine();
      </span><span style="color: #0000FF;">return</span><span style="color: #000000;"> key </span><span style="color: #000000;">-</span><span style="color: #000000;"> </span><span style="color: #800000;">'</span><span style="color: #800000;">1</span><span style="color: #800000;">'</span><span style="color: #000000;">;
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">bool</span><span style="color: #000000;"> ReadConfiguration()
    {
      </span><span style="color: #0000FF;">try</span><span style="color: #000000;">
      {
        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Set window size</span><span style="color: #008000;">
</span><span style="color: #000000;">        Console.SetWindowSize(</span><span style="color: #800080;">120</span><span style="color: #000000;">, </span><span style="color: #800080;">40</span><span style="color: #000000;">);

        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Read connectionString setting</span><span style="color: #008000;">
</span><span style="color: #000000;">        connectionString </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConfigurationManager.AppSettings[ConnectionString];
        </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.IsNullOrWhiteSpace(connectionString))
        {
          </span><span style="color: #0000FF;">throw</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> ArgumentException(ConnectionStringCannotBeNull);
        }
        PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(ParameterFormat, ConnectionString, connectionString));

        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Read messageSizeInBytes setting</span><span style="color: #008000;">
</span><span style="color: #000000;">        </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> value;
        var setting </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConfigurationManager.AppSettings[MessageSizeInBytes];
        messageSizeInBytes </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">int</span><span style="color: #000000;">.TryParse(setting, </span><span style="color: #0000FF;">out</span><span style="color: #000000;"> value) </span><span style="color: #000000;">?</span><span style="color: #000000;">
                value :
                DefaultMessageSizeInBytes;
        PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(ParameterFormat, MessageSizeInBytes, messageSizeInBytes));

        </span><span style="color: #008000;">//</span><span style="color: #008000;"> Read messageCountInBatch setting</span><span style="color: #008000;">
</span><span style="color: #000000;">        setting </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConfigurationManager.AppSettings[MessageCountInBatch];
        messageCountInBatch </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">int</span><span style="color: #000000;">.TryParse(setting, </span><span style="color: #0000FF;">out</span><span style="color: #000000;"> value) </span><span style="color: #000000;">?</span><span style="color: #000000;">
                value :
                DefaultMessageCountInBatch;
        PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.Format(ParameterFormat, MessageCountInBatch, messageCountInBatch));
        </span><span style="color: #0000FF;">return</span><span style="color: #000000;"> </span><span style="color: #0000FF;">true</span><span style="color: #000000;">;
      }
      </span><span style="color: #0000FF;">catch</span><span style="color: #000000;"> (Exception ex)
      {
        PrintException(ex);
      }
      </span><span style="color: #0000FF;">return</span><span style="color: #000000;"> </span><span style="color: #0000FF;">false</span><span style="color: #000000;">;
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">void</span><span style="color: #000000;"> PrintMessage(</span><span style="color: #0000FF;">string</span><span style="color: #000000;"> message, [CallerMemberName] </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> memberName </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">""</span><span style="color: #000000;">)
    {
      </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.IsNullOrWhiteSpace(message) </span><span style="color: #000000;">||</span><span style="color: #000000;"> </span><span style="color: #0000FF;">string</span><span style="color: #000000;">.IsNullOrWhiteSpace(memberName))
      {
        </span><span style="color: #0000FF;">return</span><span style="color: #000000;">;
      }
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">[</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Yellow;
      Console.Write(memberName);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">]</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ResetColor();
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">: </span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.WriteLine(message);
    }

    </span><span style="color: #0000FF;">private</span><span style="color: #000000;"> </span><span style="color: #0000FF;">static</span><span style="color: #000000;"> </span><span style="color: #0000FF;">void</span><span style="color: #000000;"> PrintException(Exception ex,
                       [CallerFilePath] </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> sourceFilePath </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">""</span><span style="color: #000000;">,
                       [CallerMemberName] </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> memberName </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800000;">""</span><span style="color: #000000;">,
                       [CallerLineNumber] </span><span style="color: #0000FF;">int</span><span style="color: #000000;"> sourceLineNumber </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #800080;">0</span><span style="color: #000000;">)
    {
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">[</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Yellow;
      </span><span style="color: #0000FF;">string</span><span style="color: #000000;"> fileName </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">null</span><span style="color: #000000;">;
      </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (File.Exists(sourceFilePath))
      {
        var file </span><span style="color: #000000;">=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">new</span><span style="color: #000000;"> FileInfo(sourceFilePath);
        fileName </span><span style="color: #000000;">=</span><span style="color: #000000;"> file.Name;
      }
      Console.Write(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.IsNullOrWhiteSpace(fileName) </span><span style="color: #000000;">?</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Unknown</span><span style="color: #800000;">"</span><span style="color: #000000;"> : fileName);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">:</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Yellow;
      Console.Write(</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.IsNullOrWhiteSpace(memberName) </span><span style="color: #000000;">?</span><span style="color: #000000;"> </span><span style="color: #800000;">"</span><span style="color: #800000;">Unknown</span><span style="color: #800000;">"</span><span style="color: #000000;"> : memberName);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">:</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Yellow;
      Console.Write(sourceLineNumber.ToString(CultureInfo.InvariantCulture));
      Console.ForegroundColor </span><span style="color: #000000;">=</span><span style="color: #000000;"> ConsoleColor.Green;
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">]</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.ResetColor();
      Console.Write(</span><span style="color: #800000;">"</span><span style="color: #800000;">: </span><span style="color: #800000;">"</span><span style="color: #000000;">);
      Console.WriteLine(ex </span><span style="color: #000000;">!=</span><span style="color: #000000;"> </span><span style="color: #0000FF;">null</span><span style="color: #000000;"> </span><span style="color: #000000;">&amp;&amp;</span><span style="color: #000000;"> </span><span style="color: #000000;">!</span><span style="color: #0000FF;">string</span><span style="color: #000000;">.IsNullOrWhiteSpace(ex.Message) </span><span style="color: #000000;">?</span><span style="color: #000000;"> ex.Message : </span><span style="color: #800000;">"</span><span style="color: #800000;">An error occurred.</span><span style="color: #800000;">"</span><span style="color: #000000;">);
      var aggregateException </span><span style="color: #000000;">=</span><span style="color: #000000;"> ex </span><span style="color: #0000FF;">as</span><span style="color: #000000;"> AggregateException;
      </span><span style="color: #0000FF;">if</span><span style="color: #000000;"> (aggregateException </span><span style="color: #000000;">==</span><span style="color: #000000;"> </span><span style="color: #0000FF;">null</span><span style="color: #000000;">)
      {
        </span><span style="color: #0000FF;">return</span><span style="color: #000000;">;
      }
      </span><span style="color: #0000FF;">foreach</span><span style="color: #000000;"> (var exception </span><span style="color: #0000FF;">in</span><span style="color: #000000;"> aggregateException.InnerExceptions)
      {
        PrintException(exception);
      }
    }
    </span><span style="color: #0000FF;">#endregion</span><span style="color: #000000;">
  }
}</span></pre><!-- Code inserted with Steve Dunn's Windows Live Writer Code Formatter Plugin.  http://dunnhq.com --></div>
</div>
