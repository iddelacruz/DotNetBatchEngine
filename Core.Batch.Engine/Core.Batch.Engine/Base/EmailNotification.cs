﻿using Core.Batch.Engine.Contracts;
using Core.Batch.Engine.Helpers;
using System;
using System.Threading.Tasks;

namespace Core.Batch.Engine.Base
{
    public sealed class EmailNotification : INotification
    {
        public Task NotifyAsync(NotificationType type)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
