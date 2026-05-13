using Licensing.Portal.Models;
using System.Security.Cryptography;
using System.Text;

namespace Licensing.Portal.Services
{
    public class DealerService
    {
        private readonly AzureTableStorageService _azureTableStorageService;
        private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
        private const string Digits = "0123456789";
        private const string SpecialChars = "!@#$%^&*";

        public DealerService(AzureTableStorageService azureTableStorageService)
        {
            _azureTableStorageService = azureTableStorageService;
        }

        /// <summary>
        /// Creates a new dealer with generated code and temporary password
        /// </summary>
        public async Task<(bool Success, string Message, Dealer? CreatedDealer, string? TemporaryPassword)> CreateDealerAsync(Dealer dealer)
        {
            try
            {
                // Generate unique dealer code
                dealer.DealerCode = await GenerateUniqueDealerCodeAsync(
                    dealer.DealerName,
                    dealer.City,
                    dealer.Pincode,
                    dealer.InternalDealerId
                );

                // Generate temporary password
                string tempPassword = GenerateTemporaryPassword();
                dealer.TemporaryPassword = HashPassword(tempPassword);
                dealer.PasswordChangeRequired = true;
                dealer.CreatedDate = DateTime.UtcNow;
                dealer.LicenseSequence = 0;

                if (await _azureTableStorageService.GetDealerByInternalDealerIdAsync(dealer.InternalDealerId) == null)
                {
                    // Add to Azure Table Storage
                    await _azureTableStorageService.AddDealerAsync(dealer);

                    return (true, "Dealer created successfully", dealer, tempPassword);
                }
               else
                {
                    return (false, $"Error creating dealer:Internal Dealer Id {dealer.InternalDealerId} already used ", null, null);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Error creating dealer: {ex.Message}", null, null);
            }
        }

        /// <summary>
        /// Synchronous version of CreateDealer for backward compatibility
        /// </summary>
        public (bool Success, string Message, Dealer? CreatedDealer, string? TemporaryPassword) CreateDealer(Dealer dealer)
        {
            return CreateDealerAsync(dealer).Result;
        }

        /// <summary>
        /// Generates a unique dealer code based on dealer information
        /// </summary>
        public async Task<string> GenerateUniqueDealerCodeAsync(string dealerName, string city, string pincode, string internalDealerId)
        {
            string dealerCode;
            bool isUnique = false;

            do
            {
                // Combine inputs for hash generation
                string input = $"{dealerName}{city}{pincode}{internalDealerId}{Guid.NewGuid()}";

                // Generate hash from the combined input
                using (SHA256 sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                    // Convert hash to a numeric string and take first 8 digits
                    long hashValue = Math.Abs(BitConverter.ToInt64(hashBytes, 0));
                    dealerCode = (hashValue % 100000000).ToString("D8");
                }

                // Check if the code is unique in Azure Table Storage
                isUnique = !await _azureTableStorageService.DealerExistsAsync(dealerCode);

            } while (!isUnique);

            return dealerCode;
        }

        /// <summary>
        /// Synchronous version for backward compatibility
        /// </summary>
        public string GenerateUniqueDealerCode(string dealerName, string city, string pincode, string internalDealerId)
        {
            return GenerateUniqueDealerCodeAsync(dealerName, city, pincode, internalDealerId).Result;
        }

        /// <summary>
        /// Gets a dealer by dealer code
        /// </summary>
        public async Task<Dealer?> GetDealerAsync(string dealerCode)
        {
            var dealerEntity = await _azureTableStorageService.GetDealerAsync(dealerCode);
            return dealerEntity?.ToDealer();
        }

        /// <summary>
        /// Gets all dealers
        /// </summary>
        public async Task<List<Dealer>> GetAllDealersAsync()
        {
            var dealerEntities = await _azureTableStorageService.GetAllDealersAsync();
            return dealerEntities.Select(e => e.ToDealer()).OrderByDescending(d => d.CreatedDate).ToList();
        }

        /// <summary>
        /// Updates a dealer's password
        /// </summary>
        public async Task<bool> UpdateDealerPasswordAsync(string dealerCode, string newPassword, bool requireChange = false)
        {
            try
            {
                var dealer = await GetDealerAsync(dealerCode);
                if (dealer == null) return false;

                dealer.Password = HashPassword(newPassword);
                dealer.PasswordChangeRequired = requireChange;

                await _azureTableStorageService.UpsertDealerAsync(dealer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Resets dealer's temporary password
        /// </summary>
        public async Task<(bool Success, string? NewPassword)> ResetDealerPasswordAsync(string dealerCode)
        {
            try
            {
                var dealer = await GetDealerAsync(dealerCode);
                if (dealer == null) return (false, null);

                string newTempPassword = GenerateTemporaryPassword();
                dealer.TemporaryPassword = HashPassword(newTempPassword);
                dealer.PasswordChangeRequired = true;

                await _azureTableStorageService.UpsertDealerAsync(dealer);
                return (true, newTempPassword);
            }
            catch
            {
                return (false, null);
            }
        }

        /// <summary>
        /// Increments the license sequence for a dealer
        /// </summary>
        public async Task<int> IncrementLicenseSequenceAsync(string dealerCode)
        {
            var dealer = await GetDealerAsync(dealerCode);
            
            if (dealer == null)
            {
                throw new InvalidOperationException($"Dealer with code {dealerCode} not found.");
            }

            dealer.LicenseSequence++;
            await _azureTableStorageService.UpsertDealerAsync(dealer);
            
            return dealer.LicenseSequence;
        }

        /// <summary>
        /// Synchronous version
        /// </summary>
        public int IncrementLicenseSequence(string dealerCode)
        {
            return IncrementLicenseSequenceAsync(dealerCode).Result;
        }

        /// <summary>
        /// Updates an existing dealer without fetching current data
        /// Assumes system-managed fields are already populated in the dealer object
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateDealerAsync(Dealer dealer)
        {
            try
            {
                var existingDealer = await _azureTableStorageService.GetDealerByInternalDealerIdAsync(dealer.InternalDealerId);
                if (existingDealer != null)
                {
                    return (false, $"Error creating dealer:Internal Dealer Id {dealer.InternalDealerId} already used");
                }

                await _azureTableStorageService.UpsertDealerAsync(dealer);
                return (true, "Dealer updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating dealer: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates an existing dealer with validation
        /// Fetches current dealer to preserve system-managed fields
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateDealerWithValidationAsync(Dealer dealer)
        {
            try
            {
                // Verify dealer exists before updating
                var existingDealer = await GetDealerAsync(dealer.DealerCode);
                if (existingDealer == null) return (false, $"Dealer with code {dealer.DealerCode} not found");

                // Preserve system-managed fields from existing dealer
                dealer.CreatedDate = existingDealer.CreatedDate;
                dealer.TemporaryPassword = existingDealer.TemporaryPassword;
                dealer.Password = existingDealer.Password;
                dealer.PasswordChangeRequired = existingDealer.PasswordChangeRequired;
                dealer.LicenseSequence = existingDealer.LicenseSequence;

                await _azureTableStorageService.UpsertDealerAsync(dealer);
                return (true, "Dealer updated successfully");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating dealer: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a dealer by dealer code
        /// </summary>
        public async Task<bool> DeleteDealerAsync(string dealerCode)
        {
            try
            {
                // Just delete without fetching - the dealer code is the only identifier needed
                await _azureTableStorageService.DeleteDealerAsync(dealerCode);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a temporary password with required complexity
        /// </summary>
        public string GenerateTemporaryPassword(int length = 8)
        {
            var random = new Random();
            var password = new StringBuilder();

            // Add one uppercase letter
            password.Append(UpperCase[random.Next(UpperCase.Length)]);

            // Add one digit
            password.Append(Digits[random.Next(Digits.Length)]);

            // Add one special character
            password.Append(SpecialChars[random.Next(SpecialChars.Length)]);

            // Fill the rest with random characters from all sets
            var allChars = UpperCase + LowerCase + Digits + SpecialChars;
            for (int i = password.Length; i < length; i++)
            {
                password.Append(allChars[random.Next(allChars.Length)]);
            }

            // Shuffle the password
            return ShuffleString(password.ToString());
        }

        /// <summary>
        /// Validates password complexity requirements
        /// </summary>
        public bool ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                return false;

            bool hasUpperCase = password.Any(c => char.IsUpper(c));
            bool hasNumber = password.Any(c => char.IsDigit(c));
            bool hasSpecialChar = password.Any(c => SpecialChars.Contains(c));

            return hasUpperCase && hasNumber && hasSpecialChar;
        }

        /// <summary>
        /// Hashes a password using SHA256
        /// </summary>
        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        /// <summary>
        /// Verifies if an input password matches a hash
        /// </summary>
        public bool VerifyPassword(string inputPassword, string hash)
        {
            var hashOfInput = HashPassword(inputPassword);
            return hashOfInput.Equals(hash);
        }

        private string ShuffleString(string input)
        {
            var chars = input.ToCharArray();
            var random = new Random();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                int randomIndex = random.Next(i + 1);
                // Swap
                var temp = chars[i];
                chars[i] = chars[randomIndex];
                chars[randomIndex] = temp;
            }
            return new string(chars);
        }
    }
}
