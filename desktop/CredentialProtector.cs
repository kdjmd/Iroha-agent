using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace IrohaAgentDesktop
{
    internal sealed class CredentialLoadResult
    {
        public bool NeedsMigration { get; set; }
        public bool HadDecryptionFailure { get; set; }
    }

    internal static class ApiKeyProtector
    {
        internal const string ProtectedPrefix = "iroha-dpapi:v1:";
        private static readonly byte[] OptionalEntropy = Encoding.UTF8.GetBytes("IrohaAgent.ApiKeys.v1");

        public static void ProtectSettingsForStorage(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            settings.ApiKey = ProtectValue(settings.ApiKey);
            settings.BraveSearchApiKey = ProtectValue(settings.BraveSearchApiKey);
            if (settings.ProviderApiKeys == null) return;

            var providerIds = new List<string>(settings.ProviderApiKeys.Keys);
            foreach (string providerId in providerIds)
            {
                settings.ProviderApiKeys[providerId] = ProtectValue(settings.ProviderApiKeys[providerId]);
            }
        }

        public static CredentialLoadResult UnprotectSettingsAfterLoad(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");

            var result = new CredentialLoadResult();
            settings.ApiKey = UnprotectValue(settings.ApiKey, result);
            settings.BraveSearchApiKey = UnprotectValue(settings.BraveSearchApiKey, result);
            if (settings.ProviderApiKeys != null)
            {
                var providerIds = new List<string>(settings.ProviderApiKeys.Keys);
                foreach (string providerId in providerIds)
                {
                    settings.ProviderApiKeys[providerId] = UnprotectValue(settings.ProviderApiKeys[providerId], result);
                }
            }
            return result;
        }

        internal static bool IsProtected(string value)
        {
            return !string.IsNullOrWhiteSpace(value) &&
                value.StartsWith(ProtectedPrefix, StringComparison.Ordinal);
        }

        private static string ProtectValue(string value)
        {
            string plainText = value ?? "";
            if (plainText.Length == 0 || IsProtected(plainText)) return plainText;

            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
            byte[] protectedBytes = null;
            try
            {
                protectedBytes = ProtectedData.Protect(
                    plainBytes,
                    OptionalEntropy,
                    DataProtectionScope.CurrentUser);
                return ProtectedPrefix + Convert.ToBase64String(protectedBytes);
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Windows 无法安全加密 API Key，请确认当前用户配置可用。", ex);
            }
            finally
            {
                Array.Clear(plainBytes, 0, plainBytes.Length);
                if (protectedBytes != null) Array.Clear(protectedBytes, 0, protectedBytes.Length);
            }
        }

        private static string UnprotectValue(string value, CredentialLoadResult result)
        {
            string stored = value ?? "";
            if (stored.Length == 0) return "";
            if (!IsProtected(stored))
            {
                result.NeedsMigration = true;
                return stored;
            }

            byte[] protectedBytes = null;
            byte[] plainBytes = null;
            try
            {
                protectedBytes = Convert.FromBase64String(stored.Substring(ProtectedPrefix.Length));
                plainBytes = ProtectedData.Unprotect(
                    protectedBytes,
                    OptionalEntropy,
                    DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException)
            {
                result.HadDecryptionFailure = true;
                return "";
            }
            catch (CryptographicException)
            {
                result.HadDecryptionFailure = true;
                return "";
            }
            finally
            {
                if (protectedBytes != null) Array.Clear(protectedBytes, 0, protectedBytes.Length);
                if (plainBytes != null) Array.Clear(plainBytes, 0, plainBytes.Length);
            }
        }
    }
}
