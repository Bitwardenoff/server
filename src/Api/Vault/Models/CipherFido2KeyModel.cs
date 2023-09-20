﻿using System.ComponentModel.DataAnnotations;
using Bit.Core.Utilities;
using Bit.Core.Vault.Models.Data;

namespace Bit.Api.Vault.Models;

public class CipherFido2KeyModel
{
    public CipherFido2KeyModel() { }

    public CipherFido2KeyModel(CipherLoginFido2KeyData data)
    {
        CredentialId = data.CredentialId;
        KeyType = data.KeyType;
        KeyAlgorithm = data.KeyAlgorithm;
        KeyCurve = data.KeyCurve;
        KeyValue = data.KeyValue;
        RpId = data.RpId;
        RpName = data.RpName;
        UserHandle = data.UserHandle;
        UserDisplayName = data.UserDisplayName;
        Counter = data.Counter;
        Discoverable = data.Discoverable;
        CreationDate = data.CreationDate;
    }

    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string CredentialId { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string KeyType { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string KeyAlgorithm { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string KeyCurve { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string KeyValue { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string RpId { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string RpName { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string UserHandle { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string UserDisplayName { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string Counter { get; set; }
    [EncryptedString]
    [EncryptedStringLength(1000)]
    public string Discoverable { get; set; }
    [Required]
    public DateTime CreationDate { get; set; }

    public CipherLoginFido2KeyData ToCipherLoginFido2KeyData()
    {
        return new CipherLoginFido2KeyData
        {
            CredentialId = CredentialId,
            KeyType = KeyType,
            KeyAlgorithm = KeyAlgorithm,
            KeyCurve = KeyCurve,
            KeyValue = KeyValue,
            RpId = RpId,
            RpName = RpName,
            UserHandle = UserHandle,
            UserDisplayName = UserDisplayName,
            Counter = Counter,
            Discoverable = Discoverable,
            CreationDate = CreationDate
        };
    }
}

static class CipherFido2KeyModelExtensions
{
    public static CipherLoginFido2KeyData[] ToCipherLoginFido2KeyData(this CipherFido2KeyModel[] models)
    {
        return models.Select(m => m.ToCipherLoginFido2KeyData()).ToArray();
    }
}
