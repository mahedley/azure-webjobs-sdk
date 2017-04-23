﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Azure.WebJobs.Host.Queues.Listeners;
using Newtonsoft.Json;

namespace Microsoft.Azure.WebJobs.Host
{
    /// <summary>
    /// Represents configuration for <see cref="QueueTriggerAttribute"/>.
    /// </summary>
    public sealed class JobHostQueuesConfiguration : IQueueConfiguration
    {
        private const int DefaultMaxDequeueCount = 5;
        private const int DefaultBatchSize = 16;

        // Azure Queues currently limits the number of messages retrieved to 32. We enforce this constraint here because
        // the runtime error message the user would receive from the SDK otherwise is not as helpful.
        private const int MaxBatchSize = 32;

        private const int DefaultRetryCount = 0;

        private int _batchSize = DefaultBatchSize;
        private int _newBatchThreshold;
        private TimeSpan _maxPollingInterval = QueuePollingIntervals.DefaultMaximum;
        private TimeSpan _visibilityTimeout = TimeSpan.Zero;
        private int _maxDequeueCount = DefaultMaxDequeueCount;
        private int _deleteRetryCount = DefaultRetryCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobHostQueuesConfiguration"/> class.
        /// </summary>
        internal JobHostQueuesConfiguration()
        {
            _newBatchThreshold = -1;
            QueueProcessorFactory = new DefaultQueueProcessorFactory();
        }

        /// <summary>
        /// Gets or sets the number of queue messages to retrieve and process in parallel (per job method).
        /// </summary>
        public int BatchSize
        {
            get { return _batchSize; }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                if (value > MaxBatchSize)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _batchSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the threshold at which a new batch of messages will be fetched.
        /// </summary>
        public int NewBatchThreshold
        {
            get
            {
                if (_newBatchThreshold == -1)
                {
                    // if this hasn't been set explicitly, default it
                    return _batchSize / 2;
                }
                return _newBatchThreshold;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException("value");
                }

                _newBatchThreshold = value;
            }
        }

        /// <summary>
        /// Gets or sets the longest period of time to wait before checking for a message to arrive when a queue remains
        /// empty.
        /// </summary>
        [JsonIgnore]
        public TimeSpan MaxPollingInterval
        {
            get { return _maxPollingInterval; }

            set
            {
                if (value < QueuePollingIntervals.Minimum)
                {
                    string message = String.Format(CultureInfo.CurrentCulture,
                        "MaxPollingInterval must not be less than {0}.", QueuePollingIntervals.Minimum);
                    throw new ArgumentException(message, "value");
                }

                _maxPollingInterval = value;
            }
        }

        // Host.json serializes MaxPollingInterval as an integer, not a timespan. 
        [JsonProperty("MaxPollingInterval")]
        private int MaxPollingIntervalInt
        {
            get
            {
                return (int)this.MaxPollingInterval.TotalMilliseconds;
            }
            set
            {
                this.MaxPollingInterval = TimeSpan.FromMilliseconds((int)value);
            }
        }

        /// <summary>
        /// Gets or sets the number of times to try processing a message before moving it to the poison queue (where
        /// possible).
        /// </summary>
        /// <remarks>
        /// Some queues do not have corresponding poison queues, and this property does not apply to them. Specifically,
        /// there are no corresponding poison queues for any queue whose name already ends in "-poison" or any queue
        /// whose name is already too long to add a "-poison" suffix.
        /// </remarks>
        public int MaxDequeueCount
        {
            get { return _maxDequeueCount; }

            set
            {
                if (value < 1)
                {
                    throw new ArgumentException("MaxDequeueCount must not be less than 1.", "value");
                }

                _maxDequeueCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the default message visibility timeout that will be used
        /// for messages that fail processing. The default is TimeSpan.Zero. To increase
        /// the time delay between retries, increase this value.
        /// </summary>
        /// <remarks>
        /// When message processing fails, the message will remain in the queue and
        /// its visibility will be updated with this value. The message will then be
        /// available for reprocessing after this timeout expires.
        /// </remarks>
        public TimeSpan VisibilityTimeout
        {
            get
            {
                return _visibilityTimeout;
            }
            set
            {
                _visibilityTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="IQueueProcessorFactory"/> that will be used to create
        /// <see cref="QueueProcessor"/> instances that will be used to process messages.
        /// </summary>
        [CLSCompliant(false)]
        public IQueueProcessorFactory QueueProcessorFactory
        {
            get;
            set;
        }

        /// <summary>
        /// Get's or sets the delete retry count
        /// In some cases a queue may be unavailable temporarily. Retrying in some cases can
        /// reduce the chance that a successfully processed message can return to the queue.
        /// The default is 0
        /// </summary>
        public int DeleteRetryCount
        {
            get
            {
                return _deleteRetryCount;
            }
            set
            {
                _deleteRetryCount = value;
            }
        }
    }
}
