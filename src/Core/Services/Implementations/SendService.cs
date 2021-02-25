﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bit.Core.Context;
using Bit.Core.Enums;
using Bit.Core.Exceptions;
using Bit.Core.Models.Data;
using Bit.Core.Models.Table;
using Bit.Core.Repositories;
using Bit.Core.Settings;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;

namespace Bit.Core.Services
{
    public class SendService : ISendService
    {
        private readonly ISendRepository _sendRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPolicyRepository _policyRepository;
        private readonly IUserService _userService;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly ISendFileStorageService _sendFileStorageService;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IPushNotificationService _pushService;
        private readonly GlobalSettings _globalSettings;
        private readonly ICurrentContext _currentContext;
        private const long _fileSizeLeeway = 1024;

        public SendService(
            ISendRepository sendRepository,
            IUserRepository userRepository,
            IUserService userService,
            IOrganizationRepository organizationRepository,
            ISendFileStorageService sendFileStorageService,
            IPasswordHasher<User> passwordHasher,
            IPushNotificationService pushService,
            GlobalSettings globalSettings,
            IPolicyRepository policyRepository,
            ICurrentContext currentContext)
        {
            _sendRepository = sendRepository;
            _userRepository = userRepository;
            _userService = userService;
            _policyRepository = policyRepository;
            _organizationRepository = organizationRepository;
            _sendFileStorageService = sendFileStorageService;
            _passwordHasher = passwordHasher;
            _pushService = pushService;
            _globalSettings = globalSettings;
            _currentContext = currentContext;
        }

        public async Task SaveSendAsync(Send send)
        {
            // Make sure user can save Sends
            await ValidateUserCanSaveAsync(send.UserId);

            if (send.Id == default(Guid))
            {
                await _sendRepository.CreateAsync(send);
                await _pushService.PushSyncSendCreateAsync(send);
            }
            else
            {
                send.RevisionDate = DateTime.UtcNow;
                await _sendRepository.UpsertAsync(send);
                await _pushService.PushSyncSendUpdateAsync(send);
            }
        }

        public async Task<string> SaveFileSendAsync(Send send, SendFileData data, long fileLength)
        {
            if (send.Type != SendType.File)
            {
                throw new BadRequestException("Send is not of type \"file\".");
            }

            if (fileLength < 1)
            {
                throw new BadRequestException("No file data.");
            }

            var storageBytesRemaining = await StorageRemainingForSendAsync(send);

            if (storageBytesRemaining < fileLength)
            {
                throw new BadRequestException("Not enough storage available.");
            }

            var fileId = Utilities.CoreHelpers.SecureRandomString(32, upper: false, special: false);

            try
            {
                data.Id = fileId;
                data.Size = fileLength;
                send.Data = JsonConvert.SerializeObject(data,
                    new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                await SaveSendAsync(send);
                return await _sendFileStorageService.GetSendFileUploadUrlAsync(send, fileId);
            }
            catch
            {
                // Clean up since this is not transactional
                await _sendFileStorageService.DeleteFileAsync(send, fileId);
                throw;
            }
        }

        public async Task UploadFileToExistingSendAsync(Stream stream, Send send)
        {
            if (send?.Data == null)
            {
                throw new BadRequestException("Send does not have file data");
            }

            if (send.Type != SendType.File)
            {
                throw new BadRequestException("Not a File Type Send.");
            }

            var data = JsonConvert.DeserializeObject<SendFileData>(send.Data);

            if (stream.Length < data.Size - _fileSizeLeeway || stream.Length > data.Size + _fileSizeLeeway)
            {
                throw new BadRequestException("Stream size does not match expected size.");
            }

            await _sendFileStorageService.UploadNewFileAsync(stream, send, data.Id);
        }

        public async Task ValidateSendFile(Send send)
        {
            var fileData = JsonConvert.DeserializeObject<SendFileData>(send.Data);

            var valid = await _sendFileStorageService.ValidateFile(send, fileData.Id, fileData.Size, _fileSizeLeeway);

            if (!valid)
            {
                // File reported differs in size from that promised. Must be a rogue client. Delete Send
                await DeleteSendAsync(send);
            }
        }

        public async Task DeleteSendAsync(Send send)
        {
            await _sendRepository.DeleteAsync(send);
            if (send.Type == Enums.SendType.File)
            {
                var data = JsonConvert.DeserializeObject<SendFileData>(send.Data);
                await _sendFileStorageService.DeleteFileAsync(send, data.Id);
            }
            await _pushService.PushSyncSendDeleteAsync(send);
        }

        // Response: Send, password required, password invalid
        public async Task<(Send, bool, bool)> AccessAsync(Guid sendId, string password)
        {
            var send = await _sendRepository.GetByIdAsync(sendId);
            var now = DateTime.UtcNow;
            if (send == null || send.MaxAccessCount.GetValueOrDefault(int.MaxValue) <= send.AccessCount ||
                send.ExpirationDate.GetValueOrDefault(DateTime.MaxValue) < now || send.Disabled ||
                send.DeletionDate < now)
            {
                return (null, false, false);
            }
            if (!string.IsNullOrWhiteSpace(send.Password))
            {
                if (string.IsNullOrWhiteSpace(password))
                {
                    return (null, true, false);
                }
                var passwordResult = _passwordHasher.VerifyHashedPassword(new User(), send.Password, password);
                if (passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    send.Password = HashPassword(password);
                }
                if (passwordResult == PasswordVerificationResult.Failed)
                {
                    return (null, false, true);
                }
            }
            // TODO: maybe move this to a simple ++ sproc?
            send.AccessCount++;
            await _sendRepository.ReplaceAsync(send);
            return (send, false, false);
        }

        public string HashPassword(string password)
        {
            return _passwordHasher.HashPassword(new User(), password);
        }

        private async Task ValidateUserCanSaveAsync(Guid? userId)
        {
            if (!userId.HasValue || (!_currentContext.Organizations?.Any() ?? true))
            {
                return;
            }

            var policies = await _policyRepository.GetManyByUserIdAsync(userId.Value);

            if (policies == null)
            {
                return;
            }

            foreach (var policy in policies.Where(p => p.Enabled && p.Type == PolicyType.DisableSend))
            {
                if (!_currentContext.ManagePolicies(policy.OrganizationId))
                {
                    throw new BadRequestException("Due to an Enterprise Policy, you are only able to delete an existing Send.");
                }
            }
        }

        private async Task<long> StorageRemainingForSendAsync(Send send)
        {
            var storageBytesRemaining = 0L;
            if (send.UserId.HasValue)
            {
                var user = await _userRepository.GetByIdAsync(send.UserId.Value);
                if (!await _userService.CanAccessPremium(user))
                {
                    throw new BadRequestException("You must have premium status to use file sends.");
                }

                if (user.Premium)
                {
                    storageBytesRemaining = user.StorageBytesRemaining();
                }
                else
                {
                    // Users that get access to file storage/premium from their organization get the default
                    // 1 GB max storage.
                    storageBytesRemaining = user.StorageBytesRemaining(
                        _globalSettings.SelfHosted ? (short)10240 : (short)1);
                }
            }
            else if (send.OrganizationId.HasValue)
            {
                var org = await _organizationRepository.GetByIdAsync(send.OrganizationId.Value);
                if (!org.MaxStorageGb.HasValue)
                {
                    throw new BadRequestException("This organization cannot use file sends.");
                }

                storageBytesRemaining = org.StorageBytesRemaining();
            }

            return storageBytesRemaining;
        }
    }
}
