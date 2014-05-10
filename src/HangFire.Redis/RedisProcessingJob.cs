﻿// This file is part of HangFire.
// Copyright © 2013-2014 Sergey Odinokov.
// 
// HangFire is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as 
// published by the Free Software Foundation, either version 3 
// of the License, or any later version.
// 
// HangFire is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public 
// License along with HangFire. If not, see <http://www.gnu.org/licenses/>.

using System;
using HangFire.Storage;
using ServiceStack.Redis;

namespace HangFire.Redis
{
    internal class RedisProcessingJob : IProcessingJob
    {
        private readonly IRedisClient _redis;

        public RedisProcessingJob(IRedisClient redis, string jobId, string queue)
        {
            if (redis == null) throw new ArgumentNullException("redis");
            if (jobId == null) throw new ArgumentNullException("jobId");
            if (queue == null) throw new ArgumentNullException("queue");

            _redis = redis;

            JobId = jobId;
            Queue = queue;
        }

        public string JobId { get; private set; }
        public string Queue { get; private set; }

        public void Dispose()
        {
            using (var transaction = _redis.CreateTransaction())
            {
                transaction.QueueCommand(x => x.RemoveItemFromList(
                    String.Format(RedisStorage.Prefix + "queue:{0}:dequeued", Queue),
                    JobId,
                    -1));

                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format(RedisStorage.Prefix + "job:{0}", JobId),
                    "Fetched"));
                transaction.QueueCommand(x => x.RemoveEntryFromHash(
                    String.Format(RedisStorage.Prefix + "job:{0}", JobId),
                    "Checked"));

                transaction.Commit();
            }
        }
    }
}
