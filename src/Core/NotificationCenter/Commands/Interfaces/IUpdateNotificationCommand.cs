﻿#nullable enable
using Bit.Core.NotificationCenter.Entities;

namespace Bit.Core.NotificationCenter.Commands.Interfaces;

public interface IUpdateNotificationCommand
{
    Task UpdateAsync(Notification notification);
}
