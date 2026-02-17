using Microsoft.AspNetCore.DataProtection;

namespace ProDoctivityDS.Shared.Services
{
    public class EncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider provider)
        {
            // El "propósito" actúa como una clave secundaria; asegúrate de que sea único para este módulo.
            // Si cambias este propósito, los datos previamente cifrados NO podrán descifrarse.
            _protector = provider.CreateProtector("Prodoctivity.Encryption.v1");
        }

        /// <summary>
        /// Cifra un texto plano.
        /// </summary>
        /// <param name="plainText">Texto a cifrar (puede ser null o vacío).</param>
        /// <returns>Texto cifrado en Base64.</returns>
        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                byte[] protectedBytes = _protector.Protect(plainBytes);
                return Convert.ToBase64String(protectedBytes);
            }
            catch (Exception ex)
            {
                // Loggear el error (puedes inyectar ILogger si lo deseas)
                throw new InvalidOperationException("Error al cifrar el texto.", ex);
            }
        }

        /// <summary>
        /// Descifra un texto previamente cifrado con Encrypt.
        /// </summary>
        /// <param name="cipherText">Texto cifrado en Base64.</param>
        /// <returns>Texto plano original.</returns>
        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                byte[] protectedBytes = Convert.FromBase64String(cipherText);
                byte[] plainBytes = _protector.Unprotect(protectedBytes);
                return System.Text.Encoding.UTF8.GetString(plainBytes);
            }
            catch (FormatException)
            {
                // El texto no es Base64 válido → probablemente no está cifrado.
                // En este caso, podrías retornarlo tal cual (por retrocompatibilidad) o lanzar excepción.
                // Por seguridad, lanzamos excepción.
                throw new InvalidOperationException("El texto no tiene formato de dato cifrado válido.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error al descifrar el texto.", ex);
            }
        }
    }
}

